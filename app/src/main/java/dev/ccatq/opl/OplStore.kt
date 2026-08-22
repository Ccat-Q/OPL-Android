package dev.ccatq.opl

import android.content.Context
import org.json.JSONArray
import org.json.JSONObject
import java.util.UUID

data class Tunnel(
    val id: String = UUID.randomUUID().toString(),
    val name: String,
    val peerUid: String,
    val protocol: String,
    val remotePort: Int,
    val localPort: Int,
    val enabled: Boolean = true,
)

data class OplState(val uid: String, val tunnels: List<Tunnel>)
data class Preset(val name: String, val note: String, val tunnels: List<PresetTunnel>)
data class PresetTunnel(val remotePort: Int, val localPort: Int, val protocol: String)

class OplStore(context: Context) {
    private val preferences = context.getSharedPreferences("opl", Context.MODE_PRIVATE)

    fun load(): OplState {
        val uid = preferences.getString("uid", null) ?: newUid().also { preferences.edit().putString("uid", it).apply() }
        val raw = preferences.getString("tunnels", "[]") ?: "[]"
        val tunnels = runCatching {
            JSONArray(raw).let { array ->
                List(array.length()) { index ->
                    array.getJSONObject(index).let { item ->
                        Tunnel(
                            id = item.getString("id"), name = item.getString("name"),
                            peerUid = item.getString("peerUid"), protocol = item.getString("protocol"),
                            remotePort = item.getInt("remotePort"), localPort = item.getInt("localPort"),
                            enabled = item.optBoolean("enabled", true),
                        )
                    }
                }
            }
        }.getOrDefault(emptyList())
        return OplState(uid, tunnels)
    }

    fun save(tunnels: List<Tunnel>) {
        val array = JSONArray()
        tunnels.forEach { tunnel ->
            array.put(JSONObject().apply {
                put("id", tunnel.id); put("name", tunnel.name); put("peerUid", tunnel.peerUid)
                put("protocol", tunnel.protocol); put("remotePort", tunnel.remotePort)
                put("localPort", tunnel.localPort); put("enabled", tunnel.enabled)
            })
        }
        preferences.edit().putString("tunnels", array.toString()).apply()
    }

    fun reset(): OplState {
        val uid = newUid()
        preferences.edit().putString("uid", uid).putString("tunnels", "[]").apply()
        return OplState(uid, emptyList())
    }

    private fun newUid(): String = UUID.randomUUID().toString().replace("-", "").take(12)
}

val gamePresets = listOf(
    Preset("泰拉瑞亚", "连接后使用 127.0.0.1:7776", listOf(PresetTunnel(7777, 7776, "TCP"))),
    Preset("饥荒", "在游戏控制台连接 127.0.0.1:10999", listOf(PresetTunnel(10999, 10999, "UDP"), PresetTunnel(10998, 10998, "UDP"))),
    Preset("像素工厂", "使用 IP 127.0.0.1:6568 加入", listOf(PresetTunnel(6567, 6568, "TCP"), PresetTunnel(6567, 6568, "UDP"))),
)
