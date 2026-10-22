package com.qasmartclass.student.services

import android.app.Notification
import android.app.NotificationChannel
import android.app.NotificationManager
import android.app.Service
import android.content.Context
import android.content.Intent
import android.content.pm.ServiceInfo
import android.graphics.Bitmap
import android.graphics.PixelFormat
import android.hardware.display.DisplayManager
import android.hardware.display.VirtualDisplay
import android.media.ImageReader
import android.media.projection.MediaProjection
import android.media.projection.MediaProjectionManager
import android.os.Build
import android.os.Handler
import android.os.HandlerThread
import android.os.IBinder
import android.util.DisplayMetrics
import android.util.Log
import java.io.ByteArrayOutputStream

/**
 * Chụp màn hình thiết bị học sinh (kể cả khi HS đang ở ứng dụng khác) để giáo viên
 * xem chi tiết HS đang làm gì. Cần HS bấm "Bắt đầu ngay" ở hộp thoại hệ thống 1 lần.
 */
class ScreenCaptureService : Service() {

    companion object {
        private const val TAG = "ScreenCaptureService"
        private const val CHANNEL_ID = "qa_screen_capture"
        private const val NOTIF_ID = 4201
        private const val TARGET_WIDTH = 540
        private const val FRAME_INTERVAL_MS = 150L
        const val EXTRA_CODE = "code"
        const val EXTRA_DATA = "data"

        @Volatile
        var instance: ScreenCaptureService? = null
            private set

        /** JPEG của khung hình mới nhất, hoặc null nếu chưa được cấp quyền / chưa có khung hình. */
        fun captureJpeg(): ByteArray? = instance?.encodeLatest()
    }

    private var projection: MediaProjection? = null
    private var virtualDisplay: VirtualDisplay? = null
    private var imageReader: ImageReader? = null
    private var thread: HandlerThread? = null
    private var lastFrameTime = 0L

    @Volatile
    private var latestBitmap: Bitmap? = null

    override fun onBind(intent: Intent?): IBinder? = null

    override fun onStartCommand(intent: Intent?, flags: Int, startId: Int): Int {
        startAsForeground()
        val code = intent?.getIntExtra(EXTRA_CODE, 0) ?: 0
        @Suppress("DEPRECATION")
        val data: Intent? = intent?.getParcelableExtra(EXTRA_DATA)
        if (data == null || projection != null) {
            if (projection == null) stopSelf()
            return START_NOT_STICKY
        }
        try {
            val mpm = getSystemService(Context.MEDIA_PROJECTION_SERVICE) as MediaProjectionManager
            val mp = mpm.getMediaProjection(code, data)
            projection = mp
            instance = this

            val metrics = DisplayMetrics()
            @Suppress("DEPRECATION")
            (getSystemService(Context.WINDOW_SERVICE) as android.view.WindowManager)
                .defaultDisplay.getRealMetrics(metrics)
            val scale = TARGET_WIDTH.toFloat() / metrics.widthPixels
            val w = TARGET_WIDTH
            val h = (metrics.heightPixels * scale).toInt().coerceAtLeast(1)
            val dpi = (metrics.densityDpi * scale).toInt().coerceAtLeast(60)

            val ht = HandlerThread("qa-capture").also { it.start() }
            thread = ht
            val handler = Handler(ht.looper)

            // Android 14+: bắt buộc đăng ký callback trước khi tạo VirtualDisplay
            mp.registerCallback(object : MediaProjection.Callback() {
                override fun onStop() {
                    Log.i(TAG, "MediaProjection stopped")
                    stopSelf()
                }
            }, handler)

            val reader = ImageReader.newInstance(w, h, PixelFormat.RGBA_8888, 2)
            imageReader = reader
            reader.setOnImageAvailableListener({ r ->
                val image = try { r.acquireLatestImage() } catch (e: Exception) { null } ?: return@setOnImageAvailableListener
                try {
                    val now = System.currentTimeMillis()
                    if (now - lastFrameTime < FRAME_INTERVAL_MS) return@setOnImageAvailableListener
                    lastFrameTime = now
                    val plane = image.planes[0]
                    val pixelStride = plane.pixelStride
                    val rowPadding = plane.rowStride - pixelStride * w
                    val full = Bitmap.createBitmap(w + rowPadding / pixelStride, h, Bitmap.Config.ARGB_8888)
                    full.copyPixelsFromBuffer(plane.buffer)
                    val cropped = if (full.width != w) Bitmap.createBitmap(full, 0, 0, w, h).also { full.recycle() } else full
                    latestBitmap = cropped
                } catch (e: Exception) {
                    Log.w(TAG, "frame error: ${e.message}")
                } finally {
                    image.close()
                }
            }, handler)

            virtualDisplay = mp.createVirtualDisplay(
                "QASmartClassCapture", w, h, dpi,
                DisplayManager.VIRTUAL_DISPLAY_FLAG_AUTO_MIRROR,
                reader.surface, null, handler
            )
        } catch (e: Exception) {
            Log.e(TAG, "Start capture failed: ${e.message}")
            stopSelf()
        }
        return START_NOT_STICKY
    }

    private fun encodeLatest(): ByteArray? {
        val bmp = latestBitmap ?: return null
        return try {
            val bos = ByteArrayOutputStream()
            synchronized(bmp) {
                if (bmp.isRecycled) return null
                bmp.compress(Bitmap.CompressFormat.JPEG, 55, bos)
            }
            bos.toByteArray()
        } catch (e: Exception) {
            null
        }
    }

    private fun startAsForeground() {
        val nm = getSystemService(Context.NOTIFICATION_SERVICE) as NotificationManager
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O) {
            nm.createNotificationChannel(
                NotificationChannel(CHANNEL_ID, "Chia sẻ màn hình với giáo viên", NotificationManager.IMPORTANCE_LOW)
            )
        }
        val builder = if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.O)
            Notification.Builder(this, CHANNEL_ID) else @Suppress("DEPRECATION") Notification.Builder(this)
        val notif = builder
            .setContentTitle("QA SmartClass")
            .setContentText("Đang chia sẻ màn hình với giáo viên")
            .setSmallIcon(android.R.drawable.ic_menu_view)
            .setOngoing(true)
            .build()
        if (Build.VERSION.SDK_INT >= Build.VERSION_CODES.Q) {
            startForeground(NOTIF_ID, notif, ServiceInfo.FOREGROUND_SERVICE_TYPE_MEDIA_PROJECTION)
        } else {
            startForeground(NOTIF_ID, notif)
        }
    }

    override fun onDestroy() {
        instance = null
        try { virtualDisplay?.release() } catch (_: Exception) {}
        try { imageReader?.close() } catch (_: Exception) {}
        try { projection?.stop() } catch (_: Exception) {}
        thread?.quitSafely()
        latestBitmap = null
        virtualDisplay = null
        imageReader = null
        projection = null
        super.onDestroy()
    }
}
