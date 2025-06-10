using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Engines;
using Org.BouncyCastle.Crypto.Generators;
using Org.BouncyCastle.Crypto.Modes;
using Org.BouncyCastle.Crypto.Parameters;

namespace Minio.Admin;

/// <summary>
/// MinIO Admin API加密/解密工具类
/// 
/// 实现了MinIO Admin API使用的加密格式，支持流式加密和解密。
/// 使用Argon2进行密钥派生，支持AES-GCM和ChaCha20-Poly1305两种AEAD加密算法。
/// 
/// 加密消息格式:
/// 
/// |    41 bytes HEADER      |  - 固定头部
/// |-------------------------|
/// | 16 KiB encrypted chunk  |  - 加密块 + 16字节认证标签
/// |     + 16 bytes TAG      |
/// |-------------------------|
/// |          ....           |  - 可能有多个加密块
/// |-------------------------|
/// | ~16 KiB encrypted chunk |  - 最后一个块可能小于16KB
/// |     + 16 bytes TAG      |
/// |-------------------------|
/// 
/// 头部格式 (41字节):
/// 
/// | 32 bytes salt  |  - Argon2密钥派生使用的盐值
/// |----------------|
/// | 1 byte AEAD ID |  - 加密算法标识: 0=AES-GCM, 1=ChaCha20-Poly1305
/// |----------------|
/// | 8 bytes NONCE  |  - 基础随机数，实际使用时会与块ID组合
/// |----------------|
/// </summary>
internal static class CryptoHelper
{
    #region 常量定义

    /// <summary>认证标签长度 (128位)</summary>
    private const int TagLength = 16;

    /// <summary>每个加密块的大小 (16KB)</summary>
    private const int ChunkSize = 16 * 1024;

    /// <summary>加密块的最大大小 (包含认证标签)</summary>
    private const int MaxChunkSize = ChunkSize + TagLength;

    /// <summary>盐值长度 (256位)</summary>
    private const int SaltLength = 32;

    /// <summary>随机数长度 (64位)</summary>
    private const int NonceLength = 8;

    /// <summary>AES-GCM算法标识符</summary>
    private const int AeadIdAesGcm = 0;

    /// <summary>ChaCha20-Poly1305算法标识符</summary>
    private const int AeadIdChaCha20Poly1305 = 1;

    /// <summary>最后一个块的标记位</summary>
    private const byte LastChunkMarker = 0x80;

    /// <summary>Argon2密钥长度 (256位)</summary>
    private const int KeyLength = 32;

    /// <summary>AEAD认证标签位长度</summary>
    private const int AeadTagBits = 128;

    #endregion

    #region 私有字段

    /// <summary>密码安全的随机数生成器</summary>
    private static readonly RandomNumberGenerator random = RandomNumberGenerator.Create();

    #endregion

    #region 工具方法

    /// <summary>
    /// 生成指定长度的加密安全随机字节
    /// </summary>
    /// <param name="length">要生成的随机字节长度</param>
    /// <returns>随机字节数组</returns>
    private static byte[] GenerateRandom(int length)
    {
        if (length <= 0)
            throw new ArgumentException("Length must be positive", nameof(length));

        var data = new byte[length];
        random.GetBytes(data);
        return data;
    }

    /// <summary>
    /// 将多个字节数组连接成一个数组
    /// </summary>
    /// <param name="args">要连接的字节数组</param>
    /// <returns>连接后的字节数组</returns>
    private static byte[] AppendBytes(params byte[][] args)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        if (args.Length == 0)
            return Array.Empty<byte>();

        if (args.Length == 1)
            return args[0] ?? Array.Empty<byte>();

        // 计算总长度
        var totalLength = 0;
        foreach (var arg in args)
        {
            if (arg != null)
                totalLength += arg.Length;
        }

        if (totalLength == 0)
            return Array.Empty<byte>();

        // 连接字节数组
        var result = new byte[totalLength];
        var offset = 0;
        foreach (var arg in args)
        {
            if (arg?.Length > 0)
            {
                Buffer.BlockCopy(arg, 0, result, offset, arg.Length);
                offset += arg.Length;
            }
        }
        return result;
    }

    /// <summary>
    /// 从流中完整读取指定长度的数据
    /// </summary>
    /// <param name="inputStream">输入流</param>
    /// <param name="buffer">接收缓冲区</param>
    /// <param name="raiseEof">遇到EOF时是否抛出异常</param>
    /// <returns>读取的字节数和是否遇到EOF</returns>
    private static (int bytesRead, bool eof) ReadFully(Stream inputStream, byte[] buffer, bool raiseEof)
    {
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));
        if (buffer == null)
            throw new ArgumentNullException(nameof(buffer));

        var totalBytesRead = 0;
        var eof = false;

        while (totalBytesRead < buffer.Length)
        {
            var bytesToRead = buffer.Length - totalBytesRead;
            var bytesRead = inputStream.Read(buffer, totalBytesRead, bytesToRead);

            if (bytesRead == 0)
            {
                if (raiseEof)
                    throw new EndOfStreamException("Unexpected end of stream");
                eof = true;
                break;
            }

            totalBytesRead += bytesRead;
        }

        return (totalBytesRead, eof);
    }

    #endregion

    #region 加密算法相关

    /// <summary>
    /// 创建AEAD加密/解密器
    /// </summary>
    /// <param name="encryptFlag">true=加密模式, false=解密模式</param>
    /// <param name="aeadId">AEAD算法标识符</param>
    /// <param name="key">加密密钥</param>
    /// <param name="paddedNonce">填充后的随机数</param>
    /// <returns>配置好的AEAD加密器</returns>
    /// <exception cref="NotSupportedException">不支持的AEAD算法</exception>
    private static IAeadCipher GetEncryptDecryptCipher(bool encryptFlag, int aeadId, byte[] key, byte[] paddedNonce)
    {
        if (key == null)
            throw new ArgumentNullException(nameof(key));
        if (paddedNonce == null)
            throw new ArgumentNullException(nameof(paddedNonce));

        IAeadCipher cipher = aeadId switch
        {
            AeadIdAesGcm => new GcmBlockCipher(new AesEngine()),
            AeadIdChaCha20Poly1305 => new Org.BouncyCastle.Crypto.Modes.ChaCha20Poly1305(),
            _ => throw new NotSupportedException($"Unsupported AEAD algorithm ID: {aeadId}")
        };

        // 初始化加密器，使用128位认证标签
        cipher.Init(encryptFlag, new AeadParameters(new KeyParameter(key), AeadTagBits, paddedNonce));
        return cipher;
    }

    /// <summary>
    /// 创建加密器
    /// </summary>
    private static IAeadCipher GetEncryptCipher(int aeadId, byte[] key, byte[] paddedNonce)
    {
        return GetEncryptDecryptCipher(encryptFlag: true, aeadId, key, paddedNonce);
    }

    /// <summary>
    /// 创建解密器
    /// </summary>
    private static IAeadCipher GetDecryptCipher(int aeadId, byte[] key, byte[] paddedNonce)
    {
        return GetEncryptDecryptCipher(encryptFlag: false, aeadId, key, paddedNonce);
    }

    /// <summary>
    /// 使用Argon2算法从密码和盐值派生加密密钥
    /// 
    /// Argon2参数:
    /// - 变体: Argon2id (抗侧信道攻击)
    /// - 版本: v1.3
    /// - 内存: 64MB (65536 KB)
    /// - 并行度: 4线程
    /// - 迭代次数: 1 (内存困难为主)
    /// </summary>
    /// <param name="secret">原始密码</param>
    /// <param name="salt">盐值</param>
    /// <returns>派生的32字节密钥</returns>
    private static byte[] GenerateKey(byte[] secret, byte[] salt)
    {
        if (secret == null)
            throw new ArgumentNullException(nameof(secret));
        if (salt == null)
            throw new ArgumentNullException(nameof(salt));

        var generator = new Argon2BytesGenerator();
        generator.Init(new Argon2Parameters.Builder(Argon2Parameters.Argon2id)
            .WithVersion(Argon2Parameters.Version13)    // 使用Argon2版本1.3
            .WithSalt(salt)                             // 盐值
            .WithMemoryAsKB(65536)                      // 64MB内存使用
            .WithParallelism(4)                         // 4线程并行
            .WithIterations(1)                          // 1次迭代（内存困难为主）
            .Build());

        var key = new byte[KeyLength];
        _ = generator.GenerateBytes(secret, key);
        return key;
    }

    /// <summary>
    /// 生成AEAD附加认证数据 (AAD)
    /// 
    /// 附加认证数据用于防止加密块的重排序攻击。
    /// 格式: [1字节标志位] + [16字节空加密结果的认证标签]
    /// </summary>
    /// <param name="encryptFlag">加密标志 (当前未使用，保留用于调试)</param>
    /// <param name="aeadId">AEAD算法标识符</param>
    /// <param name="key">加密密钥</param>
    /// <param name="paddedNonce">填充后的随机数</param>
    /// <returns>附加认证数据</returns>
    private static byte[] GenerateEncryptDecryptAdditionalData(bool encryptFlag, int aeadId, byte[] key, byte[] paddedNonce)
    {
        // 创建加密器并对空数据加密，获取认证标签
        var cipher = GetEncryptCipher(aeadId, key, paddedNonce);
        var additionalData = new byte[TagLength];
        _ = cipher.DoFinal(additionalData, 0);
        Debug.WriteLine($"GenerateEncryptDecryptAdditionalData-> EncryptFlag({encryptFlag})");
        // 返回 [标志位:0x00] + [认证标签]
        return AppendBytes([0], additionalData);
    }

    /// <summary>生成加密时的附加认证数据</summary>
    private static byte[] GenerateEncryptAdditionalData(int aeadId, byte[] key, byte[] paddedNonce)
    {
        return GenerateEncryptDecryptAdditionalData(encryptFlag: true, aeadId, key, paddedNonce);
    }

    /// <summary>生成解密时的附加认证数据</summary>
    private static byte[] GenerateDecryptAdditionalData(int aeadId, byte[] key, byte[] paddedNonce)
    {
        return GenerateEncryptDecryptAdditionalData(encryptFlag: false, aeadId, key, paddedNonce);
    }

    /// <summary>
    /// 标记附加认证数据为最后一个块
    /// 将标志位设置为0x80，用于防止截断攻击
    /// </summary>
    /// <param name="additionalData">要标记的附加认证数据</param>
    /// <returns>标记后的附加认证数据</returns>
    private static byte[] MarkAsLast(byte[] additionalData)
    {
        if (additionalData == null || additionalData.Length == 0)
            throw new ArgumentException("Additional data cannot be null or empty", nameof(additionalData));

        additionalData[0] = LastChunkMarker;
        return additionalData;
    }

    /// <summary>
    /// 更新随机数，将块索引附加到基础随机数后面
    /// 
    /// 格式: [8字节基础随机数] + [4字节小端序块索引]
    /// 这确保每个块使用不同的随机数，防止重放攻击
    /// </summary>
    /// <param name="nonce">基础随机数</param>
    /// <param name="idx">块索引</param>
    /// <returns>更新后的随机数</returns>
    private static byte[] UpdateNonceId(byte[] nonce, int idx)
    {
        if (nonce == null)
            throw new ArgumentNullException(nameof(nonce));

        // 将索引转换为小端序字节数组
        var idxLittleEndian = BitConverter.GetBytes(idx);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(idxLittleEndian);
        }
        return AppendBytes(nonce, idxLittleEndian);
    }

    #endregion

    #region 公共API

    /// <summary>
    /// 加密数据载荷
    /// 
    /// 加密过程:
    /// 1. 生成随机盐值和随机数
    /// 2. 使用Argon2从密码派生密钥
    /// 3. 分块加密数据 (每块16KB)
    /// 4. 每块使用不同的随机数 (基础随机数+块索引)
    /// 5. 最后一块标记为结束块
    /// </summary>
    /// <param name="payload">要加密的数据</param>
    /// <param name="password">加密密码</param>
    /// <returns>加密后的数据 (包含头部)</returns>
    /// <exception cref="ArgumentNullException">参数为null</exception>
    /// <exception cref="ArgumentException">参数无效</exception>
    public static byte[] Encrypt(string password, byte[] payload)
    {
        if (payload == null)
            throw new ArgumentNullException(nameof(payload));
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));

        // 1. 生成随机盐值和随机数
        var nonce = GenerateRandom(NonceLength);
        var salt = GenerateRandom(SaltLength);

        // 2. 派生密钥
        var key = GenerateKey(Encoding.UTF8.GetBytes(password), salt);

        // 3. 设置AEAD算法 (默认使用AES-GCM)
        var aeadId = new byte[] { AeadIdAesGcm };

        // 4. 创建填充后的随机数 (8字节随机数 + 4字节零值)
        var paddedNonce = AppendBytes(nonce, new byte[] { 0, 0, 0, 0 });

        // 5. 生成附加认证数据
        var additionalData = GenerateEncryptAdditionalData(aeadId[0], key, paddedNonce);

        // 6. 构建结果: 头部(盐值+算法ID+随机数) + 加密数据
        var result = AppendBytes(salt, aeadId, nonce);

        // 7. 分块加密
        var from = 0;
        var done = false;
        for (var nonceId = 1; !done; nonceId++)
        {
            var to = Math.Min(from + ChunkSize, payload.Length);

            // 如果是最后一块，标记附加认证数据
            if (to >= payload.Length)
            {
                additionalData = MarkAsLast(additionalData);
                done = true;
            }

            // 提取当前块
            var chunk = new byte[to - from];
            Buffer.BlockCopy(payload, from, chunk, 0, to - from);

            // 更新随机数 (添加块索引)
            paddedNonce = UpdateNonceId(nonce, nonceId);

            // 加密当前块
            var cipher = GetEncryptCipher(aeadId[0], key, paddedNonce);
            cipher.ProcessAadBytes(additionalData, 0, additionalData.Length);

            var outputLength = cipher.GetOutputSize(chunk.Length);
            var encryptedData = new byte[outputLength];
            var outputOffset = cipher.ProcessBytes(chunk, 0, chunk.Length, encryptedData, 0);
            _ = cipher.DoFinal(encryptedData, outputOffset);

            // 添加到结果
            result = AppendBytes(result, encryptedData);
            from = to;
        }

        return result;
    }

    /// <summary>
    /// 解密数据流
    /// 
    /// 这是一个便捷方法，内部使用DecryptReader进行流式解密
    /// </summary>
    /// <param name="password">解密密码</param>
    /// <param name="inputStream">加密数据流</param>
    /// <returns>解密后的数据</returns>
    /// <exception cref="InvalidDataException">数据格式错误</exception>
    /// <exception cref="CryptographicException">解密失败</exception>
    public static string Decrypt(string password, Stream inputStream)
    {
        if (string.IsNullOrEmpty(password))
            throw new ArgumentException("Password cannot be null or empty", nameof(password));
        if (inputStream == null)
            throw new ArgumentNullException(nameof(inputStream));

        using var reader = new DecryptReader(inputStream, Encoding.UTF8.GetBytes(password));
       
        var bs= reader.ReadAllBytes();

        return Encoding.UTF8.GetString(bs);
    }

    #endregion

    #region DecryptReader类

    /// <summary>
    /// 流式解密读取器
    /// 
    /// 用于逐块解密大型加密数据，避免将整个文件加载到内存中。
    /// 支持MinIO Admin API的分块加密格式。
    /// </summary>
    public class DecryptReader : IDisposable
    {
        #region 私有字段

        private readonly Stream inputStream;
        private readonly byte[] secret;
        private readonly byte[] salt = new byte[SaltLength];
        private readonly byte[] aeadId = new byte[1];
        private readonly byte[] nonce = new byte[NonceLength];
        private readonly byte[] key;
        private byte[] additionalData;
        private int count = 0;
        private readonly byte[] chunk = new byte[MaxChunkSize];
        private byte[] oneByte;
        private bool eof = false;
        private bool disposed = false;

        #endregion

        #region 构造函数

        /// <summary>
        /// 初始化解密读取器
        /// 
        /// 构造时会立即读取并解析41字节的头部信息:
        /// - 32字节盐值
        /// - 1字节AEAD算法标识符
        /// - 8字节基础随机数
        /// </summary>
        /// <param name="inputStream">加密数据流</param>
        /// <param name="secret">解密密码的字节表示</param>
        /// <exception cref="EndOfStreamException">流数据不足</exception>
        /// <exception cref="NotSupportedException">不支持的加密算法</exception>
        public DecryptReader(Stream inputStream, byte[] secret)
        {
            this.inputStream = inputStream ?? throw new ArgumentNullException(nameof(inputStream));
            this.secret = secret ?? throw new ArgumentNullException(nameof(secret));

            try
            {
                // 读取头部信息
                _ = ReadFully(this.inputStream, salt, raiseEof: true);           // 32字节盐值
                _ = ReadFully(this.inputStream, aeadId, raiseEof: true);         // 1字节算法ID
                _ = ReadFully(this.inputStream, nonce, raiseEof: true);          // 8字节随机数

                // 验证算法支持
                if (aeadId[0] is not AeadIdAesGcm and not AeadIdChaCha20Poly1305)
                {
                    throw new NotSupportedException($"Unsupported AEAD algorithm ID: {aeadId[0]}");
                }

                // 派生密钥
                key = GenerateKey(this.secret, salt);

                // 初始化附加认证数据
                var paddedNonce = AppendBytes(nonce, new byte[] { 0, 0, 0, 0 });
                additionalData = GenerateDecryptAdditionalData(aeadId[0], key, paddedNonce);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 解密单个数据块
        /// </summary>
        /// <param name="encryptedData">加密的数据块</param>
        /// <param name="lastChunk">是否为最后一个块</param>
        /// <returns>解密后的数据</returns>
        /// <exception cref="CryptographicException">解密失败或认证失败</exception>
        private byte[] Decrypt(byte[] encryptedData, bool lastChunk)
        {
            if (encryptedData == null)
                throw new ArgumentNullException(nameof(encryptedData));

            count++;

            // 如果是最后一个块，更新附加认证数据
            if (lastChunk)
            {
                additionalData = MarkAsLast(additionalData);
            }

            // 创建当前块的随机数 (基础随机数 + 块索引)
            var paddedNonce = UpdateNonceId(nonce, count);

            try
            {
                // 初始化解密器
                var cipher = GetDecryptCipher(aeadId[0], key, paddedNonce);
                cipher.ProcessAadBytes(additionalData, 0, additionalData.Length);

                // 执行解密
                var outputLength = cipher.GetOutputSize(encryptedData.Length);
                var decryptedData = new byte[outputLength];
                var outputOffset = cipher.ProcessBytes(encryptedData, 0, encryptedData.Length, decryptedData, 0);
                _ = cipher.DoFinal(decryptedData, outputOffset);

                return decryptedData;
            }
            catch (InvalidCipherTextException ex)
            {
                throw new CryptographicException($"Decryption failed for chunk {count}: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Unexpected error during decryption of chunk {count}: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 读取下一个数据块
        /// 
        /// 实现了特殊的缓冲逻辑来检测最后一个块:
        /// - 维护一个字节的前瞻缓冲区
        /// - 如果无法读取完整的MaxChunkSize数据且没有更多数据，则认为是最后一块
        /// </summary>
        /// <returns>读取的数据块</returns>
        private byte[] ReadChunk()
        {
            if (this.eof)
            {
                return [];
            }

            using var baos = new MemoryStream();

            // 如果有缓存的字节，先写入
            if (oneByte != null)
            {
                baos.Write(oneByte, 0, oneByte.Length);
            }

            // 尝试读取完整块
            var (bytesRead, eof) = ReadFully(inputStream, chunk, raiseEof: false);
            this.eof = eof;

            // 如果读取了完整块，需要检查是否还有更多数据
            if (bytesRead == chunk.Length)
            {
                if (oneByte != null)
                {
                    // 如果之前有缓存字节，当前块减少一个字节
                    bytesRead--;
                    oneByte[0] = chunk[bytesRead];
                }
                else if (!this.eof)
                {
                    // 尝试读取一个额外字节来检测是否为最后一块
                    oneByte = new byte[] { 0 };
                    var (oneByteRead, oneByteEof) = ReadFully(inputStream, oneByte, raiseEof: false);
                    this.eof = oneByteEof;
                    if (this.eof)
                        oneByte = null;
                }
            }

            baos.Write(chunk, 0, bytesRead);
            return baos.ToArray();
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 读取并解密所有剩余数据
        /// </summary>
        /// <returns>解密后的完整数据</returns>
        /// <exception cref="ObjectDisposedException">对象已释放</exception>
        /// <exception cref="CryptographicException">解密失败</exception>
        public byte[] ReadAllBytes()
        {
            if (disposed)
                throw new ObjectDisposedException(nameof(DecryptReader));

            using var baos = new MemoryStream();
            while (!eof)
            {
                var payload = ReadChunk();
                if (payload.Length > 0)
                {
                    var decrypted = Decrypt(payload, eof);
                    baos.Write(decrypted, 0, decrypted.Length);
                }
            }
            return baos.ToArray();
        }

        #endregion

        #region IDisposable实现

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            if (!disposed)
            {
                // 清理敏感数据
                if (key != null)
                    Array.Clear(key, 0, key.Length);
                if (secret != null)
                    Array.Clear(secret, 0, secret.Length);
                if (additionalData != null)
                    Array.Clear(additionalData, 0, additionalData.Length);

                disposed = true;
            }
        }

        #endregion
    }

    #endregion
}
