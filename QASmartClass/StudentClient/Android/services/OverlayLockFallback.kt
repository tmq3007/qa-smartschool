package com.qasmartclass.student.services

import android.content.Context
import android.graphics.Color
import android.graphics.PixelFormat
import android.os.Build
import android.view.Gravity
import android.view.View
import android.view.WindowManager
import android.widget.TextView

object OverlayLockFallback {
    private var overlayView: View? = null

    fun showBlockingOverlay(context: Context) {
        if (overlayView != null) return

        val wm = context.getSystemService(Context.WINDOW_SERVICE) as WindowManager
        val layoutParams = WindowManager.LayoutParams().apply {
            type = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
                WindowManager.LayoutParams.TYPE_APPLICATION_OVERLAY
            } else {
                @Suppress("DEPRECATION")
                WindowManager.LayoutParams.TYPE_PHONE
            }
            flags = (WindowManager.LayoutParams.FLAG_NOT_TOUCH_MODAL
                    or WindowManager.LayoutParams.FLAG_FULLSCREEN
                    or WindowManager.LayoutParams.FLAG_LAYOUT_IN_SCREEN
                    or WindowManager.LayoutParams.FLAG_KEEP_SCREEN_ON)
            format = PixelFormat.TRANSLUCENT
            width = WindowManager.LayoutParams.MATCH_PARENT
            height = WindowManager.LayoutParams.MATCH_PARENT
            gravity = Gravity.CENTER
        }

        val textView = TextView(context).apply {
            text = "🔒 Màn hình đang bị khóa bởi giáo viên...\nVui lòng theo dõi bài giảng!"
            textSize = 20f
            setTextColor(Color.WHITE)
            setBackgroundColor(Color.parseColor("#E53935"))
            gravity = Gravity.CENTER
            setPadding(20, 20, 20, 20)
        }

        overlayView = textView
        try {
            wm.addView(textView, layoutParams)
        } catch (e: Exception) {
            e.printStackTrace()
        }
    }

    fun removeBlockingOverlay() {
        overlayView?.let {
            try {
                val wm = it.context.getSystemService(Context.WINDOW_SERVICE) as WindowManager
                wm.removeView(it)
            } catch (e: Exception) {
                e.printStackTrace()
            }
        }
        overlayView = null
    }
}
