package com.qasmartclass.student.receiver

import android.app.admin.DeviceAdminReceiver
import android.content.Context
import android.content.Intent
import android.util.Log
import android.widget.Toast

class DeviceAdminReceiver : DeviceAdminReceiver() {
    override fun onEnabled(context: Context, intent: Intent) {
        super.onEnabled(context, intent)
        Toast.makeText(context, "QA SmartClass Device Admin: Đã kích hoạt", Toast.LENGTH_SHORT).show()
        Log.i("DeviceAdmin", "Device Owner Admin enabled successfully.")
    }

    override fun onDisabled(context: Context, intent: Intent) {
        super.onDisabled(context, intent)
        Toast.makeText(context, "QA SmartClass Device Admin: Đã hủy kích hoạt!", Toast.LENGTH_SHORT).show()
        Log.w("DeviceAdmin", "Device Owner Admin disabled!")
    }
}
