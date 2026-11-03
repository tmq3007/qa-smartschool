package com.qasmartclass.student.views

import android.content.Context
import android.graphics.*
import android.util.AttributeSet
import android.view.MotionEvent
import android.view.View

/**
 * Bảng vẽ nháp nhanh (Quick Note Canvas) trên Sidebar Màn hình 3 (Interactive Lesson Viewer).
 * Hỗ trợ học sinh ghi chú bằng ngón tay hoặc bút cảm ứng (Stylus), bảo toàn nét vẽ khi xoay màn hình.
 */
class QuickNoteCanvasView @JvmOverloads constructor(
    context: Context,
    attrs: AttributeSet? = null,
    defStyleAttr: Int = 0
) : View(context, attrs, defStyleAttr) {

    enum class ToolMode { PEN, HIGHLIGHTER, ERASER }

    private data class StrokeItem(
        val path: Path,
        val color: Int,
        val strokeWidthPx: Float,
        val alpha: Int
    )

    private val strokes = mutableListOf<StrokeItem>()
    private var activePath: Path? = null
    private var currentMode: ToolMode = ToolMode.PEN

    private val density = resources.displayMetrics.density

    private val strokePaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        strokeJoin = Paint.Join.ROUND
        strokeCap = Paint.Cap.ROUND
    }

    private val gridPaint = Paint(Paint.ANTI_ALIAS_FLAG).apply {
        style = Paint.Style.STROKE
        color = 0xFFE2E8F0.toInt()
        strokeWidth = 1f * density
    }

    fun setToolMode(mode: ToolMode) {
        currentMode = mode
    }

    fun clearCanvas() {
        strokes.clear()
        activePath = null
        invalidate()
    }

    fun hasContent(): Boolean = strokes.isNotEmpty()

    fun getStrokeCount(): Int = strokes.size

    private fun currentColor(): Int = when (currentMode) {
        ToolMode.PEN -> 0xFF0F172A.toInt()
        ToolMode.HIGHLIGHTER -> 0xFFFACC15.toInt()
        ToolMode.ERASER -> 0xFFF8FAFC.toInt()
    }

    private fun currentStrokeWidth(): Float = when (currentMode) {
        ToolMode.PEN -> 3.0f * density
        ToolMode.HIGHLIGHTER -> 12.0f * density
        ToolMode.ERASER -> 18.0f * density
    }

    private fun currentAlpha(): Int = when (currentMode) {
        ToolMode.PEN -> 255
        ToolMode.HIGHLIGHTER -> 115
        ToolMode.ERASER -> 255
    }

    override fun onDraw(canvas: Canvas) {
        super.onDraw(canvas)
        canvas.drawColor(0xFFF8FAFC.toInt())

        // Vẽ dòng kẻ ngang mờ giúp học sinh viết chữ thẳng hàng trên Tablet
        val stepPx = (28f * density).coerceAtLeast(20f)
        var y = stepPx
        while (y < height) {
            canvas.drawLine(0f, y, width.toFloat(), y, gridPaint)
            y += stepPx
        }

        for (item in strokes) {
            strokePaint.color = item.color
            strokePaint.strokeWidth = item.strokeWidthPx
            strokePaint.alpha = item.alpha
            canvas.drawPath(item.path, strokePaint)
        }

        activePath?.let { p ->
            strokePaint.color = currentColor()
            strokePaint.strokeWidth = currentStrokeWidth()
            strokePaint.alpha = currentAlpha()
            canvas.drawPath(p, strokePaint)
        }
    }

    override fun onTouchEvent(event: MotionEvent): Boolean {
        val x = event.x
        val y = event.y
        parent?.requestDisallowInterceptTouchEvent(true)

        when (event.actionMasked) {
            MotionEvent.ACTION_DOWN -> {
                val p = Path().apply { moveTo(x, y) }
                activePath = p
                invalidate()
                return true
            }
            MotionEvent.ACTION_MOVE -> {
                activePath?.lineTo(x, y)
                invalidate()
                return true
            }
            MotionEvent.ACTION_UP, MotionEvent.ACTION_CANCEL -> {
                activePath?.let { p ->
                    p.lineTo(x, y)
                    strokes.add(
                        StrokeItem(
                            path = p,
                            color = currentColor(),
                            strokeWidthPx = currentStrokeWidth(),
                            alpha = currentAlpha()
                        )
                    )
                }
                activePath = null
                invalidate()
                return true
            }
        }
        return super.onTouchEvent(event)
    }
}

