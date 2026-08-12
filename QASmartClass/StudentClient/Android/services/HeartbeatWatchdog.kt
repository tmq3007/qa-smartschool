package com.qasmartclass.student.services

import android.app.Activity
import android.os.Handler
import android.os.Looper
import serilog.Log

class HeartbeatWatchdog(private val activity: Activity, private val lockManager: BroadcastLockManager) {
    private val handler = Handler(Looper.getMainLooper())
    private var isKioskLocked = false

    private val timeoutRunnable = Runnable {
        Log.Warning("[Watchdog] Connection timeout! Releasing Kiosk lock automatically.")
        releaseLock()
    }

    fun feedHeartbeat() {
        if (isKioskLocked) {
            handler.removeCallbacks(timeoutRunnable)
            handler.postDelayed(timeoutRunnable, 10000) // 10 seconds timeout (RB-02)
        }
    }

    fun acquireLock() {
        isKioskLocked = true
        lockManager.enterKioskMode(activity)
        feedHeartbeat()
    }

    fun releaseLock() {
        isKioskLocked = false
        handler.removeCallbacks(timeoutRunnable)
        lockManager.exitKioskMode(activity)
    }
}
