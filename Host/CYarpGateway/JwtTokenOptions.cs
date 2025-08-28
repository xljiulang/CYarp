using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Security.Cryptography;

namespace CYarpGateway
{
    /// <summary>
    /// 表示tokenOptions
    /// </summary> 
    sealed class JwtTokenOptions
    {
        private TokenValidationParameters? parameters;

        /// <summary>
        /// 公钥路径
        /// </summary>
        public string PublicKeyPath { get; init; } = "jwt-keys/publickey.pem";

        /// <summary>
        /// 安全算法
        /// </summary>
        public string SecurityAlgorithm { get; init; } = SecurityAlgorithms.RsaSha256;

        /// <summary>
        /// Issuer字段
        /// </summary>
        public string? Issuer { get; init; }

        /// <summary>
        /// Audience字段
        /// </summary>
        public string? Audience { get; init; }

        /// <summary>
        /// 时钟偏差
        /// </summary>
        public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(10d);


        /// <summary>
        /// Create安全键
        /// </summary> 
        /// <returns></returns>
        public SecurityKey CreateSecurityKey()
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(this.PublicKeyPath));
            return new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// GetVerify参数
        /// </summary>
        /// <returns></returns>
        public TokenValidationParameters GetParameters()
        {
            this.parameters ??= new TokenValidationParameters
            {
                ValidateIssuer = string.IsNullOrEmpty(this.Issuer) == false,
                ValidateAudience = string.IsNullOrEmpty(this.Audience) == false,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ClockSkew = this.ClockSkew,
                ValidIssuer = this.Issuer,
                ValidAudience = this.Audience,
                IssuerSigningKey = this.CreateSecurityKey()
            };
            return this.parameters;
        }
    }
}
