package com.qasmartclass.student.services

import android.app.usage.UsageStatsManager
import android.content.Context
import android.os.Handler
import android.os.Looper
import org.json.JSONObject
import java.io.OutputStream

class FocusReporter(private val context: Context, private val outputStream: OutputStream) {
    private val handler = Handler(Looper.getMainLooper())

    private val reportRunnable = object : Runnable {
        override fun run() {
            val foregroundApp = getForegroundAppPackage()
            val isWatching = foregroundApp == "com.qasmartclass.vncviewer" 
                          || foregroundApp == "com.qasmartclass.student"

            try {
                // Protocol: Send raw string command formatted as: CMD|FOCUS_STATUS|1/0|ForegroundApp
                val focusCmd = "CMD|FOCUS_STATUS|${if (isWatching) 1 else 0}|$foregroundApp\n"
                outputStream.write(focusCmd.toByteArray(Charsets.UTF_8))
                outputStream.flush()
            } catch (ex: Exception) {
                ex.printStackTrace()
            }

            handler.postDelayed(this, 3000) // Every 3 seconds (RB-03)
        }
    }

    private fun getForegroundAppPackage(): String {
        val usm = context.getSystemService(Context.USAGE_STATS_SERVICE) as UsageStatsManager
        val endTime = System.currentTimeMillis()
        val beginTime = endTime - 5000
        val stats = usm.queryUsageStats(UsageStatsManager.INTERVAL_BEST, beginTime, endTime)
        return stats?.maxByOrNull { it.lastTimeUsed }?.packageName ?: "unknown"
    }

    fun startReporting() {
        handler.removeCallbacks(reportRunnable)
        handler.post(reportRunnable)
    }

    fun stopReporting() {
        handler.removeCallbacks(reportRunnable)
    }
}
