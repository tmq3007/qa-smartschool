package com.qasmartclass.student.services

import android.app.Activity
import android.app.admin.DevicePolicyManager
import android.content.ComponentName
import android.content.Context
import android.view.View
import android.view.WindowManager
import com.qasmartclass.student.receiver.DeviceAdminReceiver
import serilog.Log

class BroadcastLockManager(private val context: Context) {
    private val dpm = context.getSystemService(Context.DEVICE_POLICY_SERVICE) as DevicePolicyManager
    private val adminComponent = ComponentName(context, DeviceAdminReceiver::class.java)

    fun enterKioskMode(activity: Activity) {
        try {
            if (dpm.isDeviceOwnerApp(context.packageName)) {
                // Set lock task packages
                dpm.setLockTaskPackages(adminComponent, arrayOf(context.packageName, "com.qasmartclass.vncviewer"))
                activity.startLockTask()
                Log.Information("[KioskMode] Entered Lock Task Mode (Device Owner)")
            } else {
                Log.Warning("[KioskMode] App is not Device Owner. Fallback to overlay.")
                OverlayLockFallback.showBlockingOverlay(context)
            }

            // Hide status/navigation bar and keep screen on
            activity.window.addFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            activity.window.decorView.systemUiVisibility = (
                    View.SYSTEM_UI_FLAG_LAYOUT_STABLE
                    or View.SYSTEM_UI_FLAG_LAYOUT_HIDE_NAVIGATION
                    or View.SYSTEM_UI_FLAG_LAYOUT_FULLSCREEN
                    or View.SYSTEM_UI_FLAG_HIDE_NAVIGATION
                    or View.SYSTEM_UI_FLAG_FULLSCREEN
                    or View.SYSTEM_UI_FLAG_IMMERSIVE_STICKY)
        } catch (ex: Exception) {
            Log.Error("[KioskMode] Error entering kiosk mode: ${ex.message}")
        }
    }

    fun exitKioskMode(activity: Activity) {
        try {
            if (dpm.isDeviceOwnerApp(context.packageName)) {
                activity.stopLockTask()
                Log.Information("[KioskMode] Exited Lock Task Mode")
            } else {
                OverlayLockFallback.removeBlockingOverlay()
            }
            activity.window.clearFlags(WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            activity.window.decorView.systemUiVisibility = View.SYSTEM_UI_FLAG_VISIBLE
        } catch (ex: Exception) {
            Log.Error("[KioskMode] Error exiting kiosk mode: ${ex.message}")
        }
    }
}
