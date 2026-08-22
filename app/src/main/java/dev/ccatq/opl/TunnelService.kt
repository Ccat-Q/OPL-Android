package dev.ccatq.opl

import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.os.IBinder
import androidx.core.app.NotificationCompat

class TunnelService : Service() {
    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        if (intent?.action == ACTION_STOP) {
            running = false
            stopForeground(STOP_FOREGROUND_REMOVE)
            stopSelf()
        } else {
            createChannel()
            running = true
            startForeground(NOTIFICATION_ID, NotificationCompat.Builder(this, CHANNEL_ID)
                .setSmallIcon(android.R.drawable.stat_sys_upload)
                .setContentTitle("OPL 正在运行")
                .setContentText("隧道服务已启动，配置将在原生网络核心接入后生效。")
                .setOngoing(true)
                .build())
        }
        return START_NOT_STICKY
    }

    override fun onDestroy() { running = false; super.onDestroy() }

    private fun createChannel() {
        val manager = getSystemService(NotificationManager::class.java)
        manager.createNotificationChannel(NotificationChannel(CHANNEL_ID, "OPL 隧道服务", NotificationManager.IMPORTANCE_LOW))
    }

    companion object {
        private const val CHANNEL_ID = "opl_tunnel"
        private const val NOTIFICATION_ID = 1001
        private const val ACTION_STOP = "dev.ccatq.opl.STOP"
        @Volatile var running = false
            private set

        fun start(context: Context) = context.startForegroundService(Intent(context, TunnelService::class.java))
        fun stop(context: Context) = context.startService(Intent(context, TunnelService::class.java).setAction(ACTION_STOP))
    }
}
