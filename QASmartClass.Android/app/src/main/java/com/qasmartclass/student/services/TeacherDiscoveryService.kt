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
    val webPort: Int = 8080,
    val channelId: Int = 1,
    val requirePin: Boolean = false
)

class TeacherDiscoveryService {
    companion object {
        const val DISCOVERY_PORT = 29876
        const val BEACON_PREFIX = "QASC|"
        private const val TAG = "TeacherDiscovery"
    }

    @Volatile
    private var activeSocket: DatagramSocket? = null
    @Volatile
    private var isListening = false

    suspend fun discoverTeacher(timeoutMs: Int = 15000): TeacherBeaconInfo? = withContext(Dispatchers.IO) {
        var localSocket: DatagramSocket? = null
        try {
            isListening = true
            localSocket = DatagramSocket(null).apply {
                reuseAddress = true
                soTimeout = timeoutMs
                bind(InetSocketAddress(DISCOVERY_PORT))
            }
            activeSocket = localSocket

            val buffer = ByteArray(1024)
            val packet = DatagramPacket(buffer, buffer.size)

            Log.i(TAG, "Listening for UDP Beacon on port $DISCOVERY_PORT...")
            localSocket.receive(packet)

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
                    var channelId = 1
                    var requirePin = false

                    for (i in 4 until parts.size) {
                        val token = parts[i].trim()
                        if (token.startsWith("CH=", ignoreCase = true)) {
                            channelId = token.substring(3).toIntOrNull()?.coerceIn(1, 250) ?: 1
                        } else if (token.startsWith("PIN=", ignoreCase = true)) {
                            val pinVal = token.substring(4).trim()
                            requirePin = (pinVal == "1" || pinVal.equals("true", ignoreCase = true))
                        }
                    }

                    return@withContext TeacherBeaconInfo(
                        className = className,
                        teacherName = teacherName,
                        teacherIp = serverIp,
                        tcpPort = tcpPort,
                        webPort = 8080,
                        channelId = channelId,
                        requirePin = requirePin
                    )
                }
            }
        } catch (e: kotlinx.coroutines.CancellationException) {
            throw e
        } catch (e: Exception) {
            if (isListening) {
                Log.w(TAG, "Discovery timed out or error: ${e.message}")
            }
        } finally {
            try {
                localSocket?.close()
            } catch (_: Exception) {}
            if (activeSocket === localSocket) {
                activeSocket = null
                isListening = false
            }
        }
        return@withContext null
    }

    /**
     * Quét toàn bộ các lớp học đang phát tín hiệu UDP trong khoảng thời gian [windowMs]
     * để nếu có từ 2 giáo viên trở lên cùng dạy trong mạng LAN, học sinh có thể chọn đúng phòng học.
     */
    suspend fun discoverMultipleTeachers(windowMs: Int = 2200): List<TeacherBeaconInfo> = withContext(Dispatchers.IO) {
        val discovered = LinkedHashMap<String, TeacherBeaconInfo>()
        val startMs = System.currentTimeMillis()
        var localSocket: DatagramSocket? = null
        try {
            isListening = true
            localSocket = DatagramSocket(null).apply {
                reuseAddress = true
                soTimeout = 700
                bind(InetSocketAddress(DISCOVERY_PORT))
            }
            activeSocket = localSocket

            val buffer = ByteArray(1024)
            while (isListening && (System.currentTimeMillis() - startMs) < windowMs) {
                try {
                    val packet = DatagramPacket(buffer, buffer.size)
                    localSocket.receive(packet)
                    val message = String(packet.data, 0, packet.length, Charsets.UTF_8).trim()
                    if (message.startsWith(BEACON_PREFIX)) {
                        val payload = message.substring(BEACON_PREFIX.length)
                        val parts = payload.split("|")
                        if (parts.size >= 4) {
                            val className = parts[0]
                            val teacherName = parts[1]
                            val serverIp = if (parts[2].isNotBlank()) parts[2] else packet.address.hostAddress ?: ""
                            val tcpPort = parts[3].toIntOrNull() ?: 29877
                            var channelId = 1
                            var requirePin = false

                            for (i in 4 until parts.size) {
                                val token = parts[i].trim()
                                if (token.startsWith("CH=", ignoreCase = true)) {
                                    channelId = token.substring(3).toIntOrNull()?.coerceIn(1, 250) ?: 1
                                } else if (token.startsWith("PIN=", ignoreCase = true)) {
                                    val pinVal = token.substring(4).trim()
                                    requirePin = (pinVal == "1" || pinVal.equals("true", ignoreCase = true))
                                }
                            }

                            if (serverIp.isNotBlank()) {
                                val key = "$serverIp:$tcpPort:$channelId"
                                discovered[key] = TeacherBeaconInfo(
                                    className = className,
                                    teacherName = teacherName,
                                    teacherIp = serverIp,
                                    tcpPort = tcpPort,
                                    webPort = 8080,
                                    channelId = channelId,
                                    requirePin = requirePin
                                )
                            }
                        }
                    }
                } catch (_: java.net.SocketTimeoutException) {
                    if (discovered.isNotEmpty() && (System.currentTimeMillis() - startMs) >= 1200) {
                        break
                    }
                }
            }
        } catch (e: kotlinx.coroutines.CancellationException) {
            throw e
        } catch (e: Exception) {
            if (isListening) {
                Log.w(TAG, "discoverMultipleTeachers error: ${e.message}")
            }
        } finally {
            try {
                localSocket?.close()
            } catch (_: Exception) {}
            if (activeSocket === localSocket) {
                activeSocket = null
                isListening = false
            }
        }
        return@withContext discovered.values.toList()
    }

    fun stopListening() {
        isListening = false
        val s = activeSocket
        activeSocket = null
        try {
            s?.close()
        } catch (_: Exception) {
            // Ignore
        }
    }
}
