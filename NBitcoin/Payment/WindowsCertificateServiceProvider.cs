﻿#if WIN
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace NBitcoin.Payment
{
	public class WindowsCertificateServiceProvider : ICertificateServiceProvider
	{
		public class WindowsHashChecker : ISignatureChecker
		{
			#region IHashChecker Members

			public bool VerifySignature(byte[] certificate, byte[] hash, string hashOID, byte[] signature)
			{
				return ((RSACryptoServiceProvider)new X509Certificate2(certificate).PublicKey.Key).VerifyHash(hash, hashOID, signature);
			}

			#endregion
		}
		public class WindowsSigner : ISigner
		{
			#region ISigner Members

			public byte[] Sign(byte[] certificate, byte[] hash, string hashOID)
			{
				var cert = new X509Certificate2(certificate);
				var privateKey = cert.PrivateKey as RSACryptoServiceProvider;
				if(privateKey == null)
					throw new ArgumentException("Private key not present in the certificate, impossible to sign");
				return privateKey.SignHash(hash, hashOID);
			}

			public byte[] StripPrivateKey(byte[] certificate)
			{
				return new X509Certificate2(new X509Certificate2(certificate).Export(X509ContentType.Cert)).GetRawCertData();
			}

			#endregion
		}
		public class WindowsChainChecker : IChainChecker
		{
			public WindowsChainChecker()
			{
				VerificationFlags = X509VerificationFlags.NoFlag;
			}
			public X509VerificationFlags VerificationFlags
			{
				get;
				set;
			}

			#region IChainChecker Members

			public bool VerifyChain(byte[] certificate, byte[][] additionalCertificates)
			{
				X509Chain chain;
				return VerifyChain(out chain, new X509Certificate2(certificate), additionalCertificates.Select(c => new X509Certificate2(c)).ToArray());
			}

			public bool VerifyChain(out X509Chain chain, X509Certificate2 certificate, X509Certificate2[] additionalCertificates)
			{
				chain = new X509Chain();
				chain.ChainPolicy.VerificationFlags = VerificationFlags;
				foreach(var additional in additionalCertificates)
					chain.ChainPolicy.ExtraStore.Add(additional);
				return chain.Build(certificate);
			}

			#endregion
		}

		readonly X509VerificationFlags _VerificationFlags;
		public X509VerificationFlags VerificationFlags
		{
			get
			{
				return _VerificationFlags;
			}
		}
		public WindowsCertificateServiceProvider(X509VerificationFlags verificationFlags = X509VerificationFlags.NoFlag)
		{
			_VerificationFlags = verificationFlags;
		}
		#region ICertificateServiceProvider Members

		public IChainChecker GetChainChecker()
		{
			return new WindowsChainChecker()
			{
				VerificationFlags = _VerificationFlags
			};
		}

		public ISignatureChecker GetSignatureChecker()
		{
			return new WindowsHashChecker();
		}

		public ISigner GetSigner()
		{
			return new WindowsSigner();
		}

		#endregion
	}
}
#endif