package com.qasmartclass.student.receiver

import android.app.admin.DeviceAdminReceiver
import android.content.Context
import android.content.Intent
import android.widget.Toast
import serilog.Log

class DeviceAdminReceiver : DeviceAdminReceiver() {
    override fun onEnabled(context: Context, intent: Intent) {
        super.onEnabled(context, intent)
        Toast.makeText(context, "QA SmartClass Device Admin: Enabled", Toast.LENGTH_SHORT).show()
        Log.Information("[DeviceAdmin] Device Owner Admin enabled successfully.")
    }

    override fun onDisabled(context: Context, intent: Intent) {
        super.onDisabled(context, intent)
        Toast.makeText(context, "QA SmartClass Device Admin: Disabled", Toast.LENGTH_SHORT).show()
        Log.Warning("[DeviceAdmin] Device Owner Admin disabled!")
    }
}
