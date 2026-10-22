package com.qasmartclass.student.crypto

import android.util.Base64
import java.io.ByteArrayInputStream
import java.io.ByteArrayOutputStream
import java.security.MessageDigest
import java.security.SecureRandom
import javax.crypto.Cipher
import javax.crypto.spec.IvParameterSpec
import javax.crypto.spec.SecretKeySpec

object CryptoHelper {
    private const val STATIC_SHARED_SALT = "QA_SmartClass_Shared_Network_Salt_2026"
    private const val KEY_SALT_SUFFIX = "_QASmartClassSalt2026"

    /**
     * Sinh khóa AES-256 từ sessionSalt động và muối tĩnh (đồng bộ với NetworkCryptoHelper.cs trên Windows).
     */
    fun deriveKey(sessionSalt: String): String {
        return if (sessionSalt.isEmpty()) {
            STATIC_SHARED_SALT
        } else {
            "${sessionSalt}_$STATIC_SHARED_SALT"
        }
    }

    /**
     * Giải mã lệnh mạng bắt đầu bằng tiền tố ENC_CMD| (đồng bộ với NetworkCryptoHelper.DecryptCommand trên Windows).
     */
    fun decryptCommand(encryptedPayload: String, sessionSalt: String): String {
        if (encryptedPayload.isEmpty()) return ""
        val payload = if (encryptedPayload.startsWith("ENC_CMD|")) {
            encryptedPayload.substring("ENC_CMD|".length)
        } else {
            encryptedPayload
        }
        val key = deriveKey(sessionSalt)
        return decrypt(payload, key)
    }

    /**
     * Giải mã AES-256-CBC với IV 16-byte ở đầu chuỗi Base64 (đồng bộ với CryptoHelper.Decrypt trên Windows).
     */
    fun decrypt(cipherText: String, key: String): String {
        if (cipherText.isEmpty() || key.isEmpty()) return ""
        return try {
            val fullCipher = Base64.decode(cipherText.trim(), Base64.DEFAULT)
            if (fullCipher.size < 16) return ""

            val iv = ByteArray(16)
            System.arraycopy(fullCipher, 0, iv, 0, 16)

            val cipherBytes = ByteArray(fullCipher.size - 16)
            System.arraycopy(fullCipher, 16, cipherBytes, 0, cipherBytes.size)

            val keyBytes = getKeyBytes(key)
            val keySpec = SecretKeySpec(keyBytes, "AES")
            val ivSpec = IvParameterSpec(iv)

            val cipher = Cipher.getInstance("AES/CBC/PKCS5Padding")
            cipher.init(Cipher.DECRYPT_MODE, keySpec, ivSpec)
            val plainBytes = cipher.doFinal(cipherBytes)

            String(plainBytes, Charsets.UTF_8).removePrefix("\uFEFF").trim()
        } catch (e: Exception) {
            ""
        }
    }

    /**
     * Mã hóa AES-256-CBC với IV 16-byte ngẫu nhiên đặt ở đầu (đồng bộ với CryptoHelper.Encrypt trên Windows).
     */
    fun encrypt(plainText: String, key: String): String {
        if (plainText.isEmpty() || key.isEmpty()) return ""
        return try {
            val keyBytes = getKeyBytes(key)
            val keySpec = SecretKeySpec(keyBytes, "AES")

            val iv = ByteArray(16)
            SecureRandom().nextBytes(iv)
            val ivSpec = IvParameterSpec(iv)

            val cipher = Cipher.getInstance("AES/CBC/PKCS5Padding")
            cipher.init(Cipher.ENCRYPT_MODE, keySpec, ivSpec)
            val cipherBytes = cipher.doFinal(plainText.toByteArray(Charsets.UTF_8))

            val fullCipher = ByteArray(iv.size + cipherBytes.size)
            System.arraycopy(iv, 0, fullCipher, 0, iv.size)
            System.arraycopy(cipherBytes, 0, fullCipher, iv.size, cipherBytes.size)

            Base64.encodeToString(fullCipher, Base64.NO_WRAP)
        } catch (e: Exception) {
            ""
        }
    }

    private fun getKeyBytes(key: String): ByteArray {
        val digest = MessageDigest.getInstance("SHA-256")
        val saltedBytes = (key + KEY_SALT_SUFFIX).toByteArray(Charsets.UTF_8)
        return digest.digest(saltedBytes)
    }
}
