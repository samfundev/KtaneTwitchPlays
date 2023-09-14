using System.Linq;
using Org.BouncyCastle.Asn1.X509;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Tls;


// TLS Client, adapted from https://stackoverflow.com/questions/68317182/how-to-create-a-tls-connection-with-bouncycastle-in-c
// TODO: allow authenticating with loaded X509 keys - not required for twitch
public class TPTlsClient : DefaultTlsClient
{
	public override TlsAuthentication GetAuthentication()
	{
		return new TPTlsAuthentication(Certificate.EmptyChain.GetCertificateList(), null, mContext);
	}
}

internal class TPTlsAuthentication : TlsAuthentication
{
	private Certificate CertChain;
	private AsymmetricKeyParameter PrivateKey;
	private TlsContext TlsContext;
	private bool AuthenticateClient;

	public TPTlsAuthentication(X509CertificateStructure[] certChain, AsymmetricKeyParameter privateKey, TlsContext context, bool authenticateClient = false)
	{
		CertChain = new Certificate(certChain);
		TlsContext = context;
		PrivateKey = privateKey;
		AuthenticateClient = authenticateClient;
	}

	public TlsCredentials GetClientCredentials(CertificateRequest certificateRequest)
	{
		if (AuthenticateClient)
		{
			byte[] certificateTypes = certificateRequest.CertificateTypes;
			if (certificateTypes == null || !certificateTypes.Contains(ClientCertificateType.rsa_sign))
				return null;

			SignatureAndHashAlgorithm sigHashAlgorithm = null;
			if (certificateRequest.SupportedSignatureAlgorithms != null)
			{
				foreach (SignatureAndHashAlgorithm algorithm in certificateRequest.SupportedSignatureAlgorithms)
				{
					if (algorithm.Signature == SignatureAlgorithm.rsa)
					{
						sigHashAlgorithm = algorithm;
						break;
					}
				}

				if (sigHashAlgorithm == null)
					return null;
			}

			TlsCredentials credentials = new DefaultTlsSignerCredentials(TlsContext, CertChain, PrivateKey, sigHashAlgorithm);
			return credentials;
		}
		else
			return null;
	}

	public void NotifyServerCertificate(Certificate serverCertificate)
	{
		//we are supposed to validate the server certificate here, but there's literally nothing we can do to do that
	}
}
