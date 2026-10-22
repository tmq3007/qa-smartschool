package com.qasmartclass.student.services

import android.app.usage.UsageStatsManager
import android.content.Context
import android.os.Handler
import android.os.Looper
import java.io.OutputStream

class FocusReporter(private val context: Context, private val outputStreamProvider: () -> OutputStream?) {
    private val handler = Handler(Looper.getMainLooper())
    // Ghi socket phải ở luồng nền (tránh NetworkOnMainThreadException)
    private val netExecutor = java.util.concurrent.Executors.newSingleThreadExecutor()

    /** Trang/tab đang mở trong app học sinh (gửi kèm để GV xem HS đang ở đâu). */
    @Volatile
    var currentPage: String = ""

    /** Activity có đang hiển thị không (đặt từ onStart/onStop) — không cần quyền Usage Access. */
    @Volatile
    var appInForeground: Boolean = true

    private val reportRunnable = object : Runnable {
        override fun run() {
            val usageApp = getForegroundAppPackage()
            val foregroundApp = if (appInForeground) context.packageName
                                else if (usageApp != context.packageName) usageApp else "unknown"
            val isWatching = appInForeground
                          || foregroundApp == "com.qasmartclass.vncviewer"
                          || foregroundApp == "com.qasmartclass.student"

            try {
                val os = outputStreamProvider()
                if (os != null) {
                    val page = currentPage.replace("|", " ").replace("\n", " ").trim()
                    val focusCmd = "CMD|FOCUS_STATUS|${if (isWatching) 1 else 0}|$foregroundApp|$page\n"
                    netExecutor.execute {
                        try {
                            os.write(focusCmd.toByteArray(Charsets.UTF_8))
                            os.flush()
                        } catch (ex: Exception) {
                            ex.printStackTrace()
                        }
                    }

                }
            } catch (ex: Exception) {
                ex.printStackTrace()
            }

            handler.postDelayed(this, 3000) // Every 3s
        }
    }

    private fun getForegroundAppPackage(): String {
        try {
            val usm = context.getSystemService(Context.USAGE_STATS_SERVICE) as? UsageStatsManager
            val endTime = System.currentTimeMillis()
            val beginTime = endTime - 5000
            val stats = usm?.queryUsageStats(UsageStatsManager.INTERVAL_BEST, beginTime, endTime)
            return stats?.maxByOrNull { it.lastTimeUsed }?.packageName ?: context.packageName
        } catch (e: Exception) {
            return context.packageName
        }
    }

    fun startReporting() {
        handler.removeCallbacks(reportRunnable)
        handler.post(reportRunnable)
    }

    fun stopReporting() {
        handler.removeCallbacks(reportRunnable)
    }
}
