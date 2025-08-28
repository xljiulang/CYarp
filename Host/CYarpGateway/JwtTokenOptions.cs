using Microsoft.IdentityModel.Tokens;
using System;
using System.IO;
using System.Security.Cryptography;

namespace CYarpGateway
{
    /// <summary>
    /// Represents JWT token options
    /// </summary> 
    sealed class JwtTokenOptions
    {
        private TokenValidationParameters? parameters;

        /// <summary>
        /// Public key path
        /// </summary>
        public string PublicKeyPath { get; init; } = "jwt-keys/publickey.pem";

        /// <summary>
        /// Security algorithm
        /// </summary>
        public string SecurityAlgorithm { get; init; } = SecurityAlgorithms.RsaSha256;

        /// <summary>
        /// Issuer field
        /// </summary>
        public string? Issuer { get; init; }

        /// <summary>
        /// Audience field
        /// </summary>
        public string? Audience { get; init; }

        /// <summary>
        /// 时钟偏差
        /// </summary>
        public TimeSpan ClockSkew { get; init; } = TimeSpan.FromMinutes(10d);


        /// <summary>
        /// Create security key
        /// </summary> 
        /// <returns></returns>
        public SecurityKey CreateSecurityKey()
        {
            var rsa = RSA.Create();
            rsa.ImportFromPem(File.ReadAllText(this.PublicKeyPath));
            return new RsaSecurityKey(rsa);
        }

        /// <summary>
        /// Get validation parameters
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
