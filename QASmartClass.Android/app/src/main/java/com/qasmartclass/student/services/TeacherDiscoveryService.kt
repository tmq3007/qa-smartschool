package com.qasmartclass.student.services

import android.util.Log
import kotlinx.coroutines.Dispatchers
import kotlinx.coroutines.withContext
import java.net.DatagramPacket
import java.net.DatagramSocket
import java.net.InetSocketAddress

data class TeacherBeaconInfo(
    val className: String,
    val teacherName: String,
    val teacherIp: String,
    val tcpPort: Int,
    val webPort: Int = 8080
)

class TeacherDiscoveryService {
    companion object {
        const val DISCOVERY_PORT = 29876
        const val BEACON_PREFIX = "QASC|"
        private const val TAG = "TeacherDiscovery"
    }

    private var socket: DatagramSocket? = null
    private var isListening = false

    suspend fun discoverTeacher(timeoutMs: Int = 15000): TeacherBeaconInfo? = withContext(Dispatchers.IO) {
        try {
            isListening = true
            socket = DatagramSocket(null).apply {
                reuseAddress = true
                soTimeout = timeoutMs
                bind(InetSocketAddress(DISCOVERY_PORT))
            }

            val buffer = ByteArray(1024)
            val packet = DatagramPacket(buffer, buffer.size)

            Log.i(TAG, "Listening for UDP Beacon on port $DISCOVERY_PORT...")
            socket?.receive(packet)

            val message = String(packet.data, 0, packet.length, Charsets.UTF_8).trim()
            Log.i(TAG, "Received beacon message: $message")

            if (message.startsWith(BEACON_PREFIX)) {
                val payload = message.substring(BEACON_PREFIX.length)
                val parts = payload.split("|")
                if (parts.size >= 4) {
                    val className = parts[0]
                    val teacherName = parts[1]
                    val serverIp = if (parts[2].isNotBlank()) parts[2] else packet.address.hostAddress ?: ""
                    val tcpPort = parts[3].toIntOrNull() ?: 29877

                    return@withContext TeacherBeaconInfo(
                        className = className,
                        teacherName = teacherName,
                        teacherIp = serverIp,
                        tcpPort = tcpPort,
                        webPort = 8080
                    )
                }
            }
        } catch (e: Exception) {
            Log.w(TAG, "Discovery timed out or error: ${e.message}")
        } finally {
            stopListening()
        }
        return@withContext null
    }

    fun stopListening() {
        isListening = false
        try {
            socket?.close()
        } catch (e: Exception) {
            // Ignore
        }
        socket = null
    }
}
