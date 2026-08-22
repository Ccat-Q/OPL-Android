package dev.ccatq.opl

import android.os.Bundle
import androidx.activity.ComponentActivity
import androidx.activity.compose.setContent
import androidx.compose.foundation.layout.Arrangement
import androidx.compose.foundation.layout.Column
import androidx.compose.foundation.layout.Row
import androidx.compose.foundation.layout.Spacer
import androidx.compose.foundation.layout.fillMaxSize
import androidx.compose.foundation.layout.fillMaxWidth
import androidx.compose.foundation.layout.height
import androidx.compose.foundation.layout.padding
import androidx.compose.foundation.lazy.LazyColumn
import androidx.compose.foundation.lazy.items
import androidx.compose.material.icons.Icons
import androidx.compose.material.icons.filled.Add
import androidx.compose.material.icons.filled.Delete
import androidx.compose.material.icons.filled.PlayArrow
import androidx.compose.material.icons.filled.Stop
import androidx.compose.material3.AlertDialog
import androidx.compose.material3.Button
import androidx.compose.material3.Card
import androidx.compose.material3.ExperimentalMaterial3Api
import androidx.compose.material3.FloatingActionButton
import androidx.compose.material3.Icon
import androidx.compose.material3.IconButton
import androidx.compose.material3.MaterialTheme
import androidx.compose.material3.OutlinedButton
import androidx.compose.material3.OutlinedTextField
import androidx.compose.material3.Scaffold
import androidx.compose.material3.SmallTopAppBar
import androidx.compose.material3.Switch
import androidx.compose.material3.Text
import androidx.compose.runtime.Composable
import androidx.compose.runtime.getValue
import androidx.compose.runtime.mutableStateOf
import androidx.compose.runtime.remember
import androidx.compose.runtime.setValue
import androidx.compose.ui.Modifier
import androidx.compose.ui.platform.LocalContext
import androidx.compose.ui.text.font.FontWeight
import androidx.compose.ui.unit.dp

class MainActivity : ComponentActivity() {
    override fun onCreate(savedInstanceState: Bundle?) {
        super.onCreate(savedInstanceState)
        setContent { MaterialTheme { OplApp() } }
    }
}

@OptIn(ExperimentalMaterial3Api::class)
@Composable
private fun OplApp() {
    val context = LocalContext.current
    val store = remember { OplStore(context) }
    var state by remember { mutableStateOf(store.load()) }
    var showEditor by remember { mutableStateOf(false) }
    var showPresets by remember { mutableStateOf(false) }
    var showReset by remember { mutableStateOf(false) }
    var running by remember { mutableStateOf(TunnelService.running) }

    fun save(tunnels: List<Tunnel>) { store.save(tunnels); state = state.copy(tunnels = tunnels) }

    Scaffold(
        topBar = { SmallTopAppBar(title = { Text("OPL Android") }) },
        floatingActionButton = { FloatingActionButton(onClick = { if (!running) showEditor = true }) { Icon(Icons.Default.Add, "新建隧道") } },
    ) { padding ->
        LazyColumn(
            modifier = Modifier.fillMaxSize().padding(padding).padding(horizontal = 16.dp),
            verticalArrangement = Arrangement.spacedBy(12.dp),
        ) {
            item {
                Spacer(Modifier.height(4.dp))
                Card(Modifier.fillMaxWidth()) {
                    Column(Modifier.padding(16.dp), verticalArrangement = Arrangement.spacedBy(8.dp)) {
                        Text("本机 UID", style = MaterialTheme.typography.labelLarge)
                        Text(state.uid, style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold)
                        Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                            OutlinedButton(onClick = { showReset = true }, enabled = !running) { Text("重置 UID") }
                            OutlinedButton(onClick = { showPresets = true }, enabled = !running) { Text("游戏预设") }
                            Button(onClick = {
                                if (running) TunnelService.stop(context) else TunnelService.start(context)
                                running = !running
                            }) {
                                Icon(if (running) Icons.Default.Stop else Icons.Default.PlayArrow, null)
                                Text(if (running) " 停止服务" else " 启动服务")
                            }
                        }
                        Text(if (running) "服务运行中" else "服务未启动", color = if (running) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.error)
                    }
                }
            }
            item { Text("隧道", style = MaterialTheme.typography.titleMedium, fontWeight = FontWeight.Bold) }
            if (state.tunnels.isEmpty()) item { Text("尚未配置隧道。点击右下角 + 新建，或从游戏预设添加。") }
            items(state.tunnels, key = { it.id }) { tunnel ->
                TunnelCard(tunnel = tunnel, locked = running, onEnabled = { enabled ->
                    save(state.tunnels.map { if (it.id == tunnel.id) it.copy(enabled = enabled) else it })
                }, onDelete = { save(state.tunnels.filterNot { it.id == tunnel.id }) })
            }
        }
    }

    if (showEditor) TunnelEditor(state.uid, state.tunnels, onDismiss = { showEditor = false }) { tunnel ->
        save(state.tunnels + tunnel); showEditor = false
    }
    if (showPresets) PresetDialog(state.uid, state.tunnels, onDismiss = { showPresets = false }) { tunnels ->
        save(state.tunnels + tunnels); showPresets = false
    }
    if (showReset) AlertDialog(onDismissRequest = { showReset = false }, title = { Text("重置 UID？") },
        text = { Text("重置会清空所有隧道配置，且无法撤销。") },
        confirmButton = { Button(onClick = { state = store.reset(); showReset = false }) { Text("确认重置") } },
        dismissButton = { OutlinedButton(onClick = { showReset = false }) { Text("取消") } })
}

@Composable
private fun TunnelCard(tunnel: Tunnel, locked: Boolean, onEnabled: (Boolean) -> Unit, onDelete: () -> Unit) {
    Card(Modifier.fillMaxWidth()) {
        Row(Modifier.padding(16.dp), horizontalArrangement = Arrangement.spacedBy(12.dp)) {
            Column(Modifier.weight(1f)) {
                Text(tunnel.name, fontWeight = FontWeight.Bold)
                Text("${tunnel.protocol}  ${tunnel.peerUid}")
                Text("远程 ${tunnel.remotePort} → 本地 127.0.0.1:${tunnel.localPort}")
                Text(if (tunnel.enabled) "已启用" else "已关闭", color = if (tunnel.enabled) MaterialTheme.colorScheme.primary else MaterialTheme.colorScheme.outline)
            }
            Column {
                Switch(checked = tunnel.enabled, onCheckedChange = onEnabled, enabled = !locked)
                IconButton(onClick = onDelete, enabled = !locked) { Icon(Icons.Default.Delete, "删除") }
            }
        }
    }
}

@Composable
private fun TunnelEditor(localUid: String, tunnels: List<Tunnel>, onDismiss: () -> Unit, onSave: (Tunnel) -> Unit) {
    var name by remember { mutableStateOf("自定义") }; var uid by remember { mutableStateOf("") }
    var protocol by remember { mutableStateOf("TCP") }; var remote by remember { mutableStateOf("") }; var local by remember { mutableStateOf("") }
    var error by remember { mutableStateOf<String?>(null) }
    AlertDialog(onDismissRequest = onDismiss, title = { Text("新建隧道") }, text = {
        Column(verticalArrangement = Arrangement.spacedBy(6.dp)) {
            OutlinedTextField(name, { name = it }, label = { Text("名称") })
            OutlinedTextField(uid, { uid = it.trim() }, label = { Text("远端 UID") })
            Row(horizontalArrangement = Arrangement.spacedBy(8.dp)) {
                OutlinedButton(onClick = { protocol = "TCP" }) { Text(if (protocol == "TCP") "✓ TCP" else "TCP") }
                OutlinedButton(onClick = { protocol = "UDP" }) { Text(if (protocol == "UDP") "✓ UDP" else "UDP") }
            }
            OutlinedTextField(remote, { remote = it.filter(Char::isDigit) }, label = { Text("远程端口") })
            OutlinedTextField(local, { local = it.filter(Char::isDigit) }, label = { Text("本地端口（留空则同远程）") })
            error?.let { Text(it, color = MaterialTheme.colorScheme.error) }
        }
    }, confirmButton = { Button(onClick = {
        val remotePort = remote.toIntOrNull(); val localPort = local.toIntOrNull() ?: remotePort
        error = when {
            name.isBlank() || uid.isBlank() -> "请填写名称和远端 UID"
            uid == localUid -> "不能连接自己的 UID"
            remotePort == null || localPort == null || remotePort !in 1..65535 || localPort !in 1..65535 -> "端口必须在 1 到 65535 之间"
            tunnels.any { it.protocol == protocol && it.localPort == localPort } -> "同协议的本地端口不能重复"
            else -> null
        }
        if (error == null) onSave(Tunnel(name = name, peerUid = uid, protocol = protocol, remotePort = remotePort!!, localPort = localPort!!))
    }) { Text("保存") } }, dismissButton = { OutlinedButton(onClick = onDismiss) { Text("取消") } })
}

@Composable
private fun PresetDialog(localUid: String, existing: List<Tunnel>, onDismiss: () -> Unit, onAdd: (List<Tunnel>) -> Unit) {
    var selected by remember { mutableStateOf<Preset?>(null) }
    var uid by remember { mutableStateOf("") }
    AlertDialog(onDismissRequest = onDismiss, title = { Text("游戏预设") }, text = {
        Column(verticalArrangement = Arrangement.spacedBy(8.dp)) {
            gamePresets.forEach { preset -> OutlinedButton(onClick = { selected = preset }) { Text(if (selected == preset) "✓ ${preset.name}" else preset.name) } }
            selected?.let { Text(it.note) }
            OutlinedTextField(uid, { uid = it.trim() }, label = { Text("远端 UID") })
        }
    }, confirmButton = { Button(onClick = {
        val preset = selected ?: return@Button
        if (uid.isNotBlank() && uid != localUid) {
            val additions = preset.tunnels.filter { item -> existing.none { it.protocol == item.protocol && it.localPort == item.localPort } }
                .map { item -> Tunnel(name = preset.name, peerUid = uid, protocol = item.protocol, remotePort = item.remotePort, localPort = item.localPort) }
            onAdd(additions)
        }
    }, enabled = selected != null && uid.isNotBlank() && uid != localUid) { Text("添加") } },
        dismissButton = { OutlinedButton(onClick = onDismiss) { Text("取消") } })
}
