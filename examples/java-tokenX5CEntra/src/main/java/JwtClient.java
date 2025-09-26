import com.nimbusds.jose.*;
import com.nimbusds.jose.crypto.*;
import com.nimbusds.jwt.*;
import org.json.*;

import javax.net.ssl.*;
import java.io.*;
import java.net.URI;
import java.net.http.*;
import java.nio.charset.StandardCharsets;
import java.security.*;
import java.security.cert.*;
import java.security.interfaces.RSAPrivateKey;
import java.time.Instant;
import java.net.ProxySelector;
import java.util.*;
import java.util.concurrent.*;

import org.bouncycastle.asn1.x500.RDN;
import org.bouncycastle.asn1.x500.X500Name;
import org.bouncycastle.asn1.x500.style.IETFUtils;
import org.bouncycastle.cert.jcajce.JcaX509CertificateHolder;
import org.bouncycastle.asn1.x500.style.BCStyle;

// import com.microsoft.aad.msal4j.*;
import com.microsoft.aad.msal4j.PublicClientApplication;
import com.microsoft.aad.msal4j.DeviceCode;
import com.microsoft.aad.msal4j.DeviceCodeFlowParameters;
import com.microsoft.aad.msal4j.IAuthenticationResult;

public class JwtClient {

    public static void main(String[] args) throws Exception {

        var clientId = "865f6ac0-cdbc-44c6-98cc-3e35c39ecb6e"; // aus der App-Registrierung
        List<String> scopes = Arrays.asList("openid", "profile", "email");

        String configUrl = "https://www.admin-shell-io.com/50001/.well-known/openid-configuration";

        HttpClient client = HttpClient.newBuilder()
                .proxy(ProxySelector.getDefault())
                .build();

        HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(configUrl))
                .GET()
                .build();

        HttpResponse<String> response = client.send(request, HttpResponse.BodyHandlers.ofString());
        JSONObject config = new JSONObject(response.body());
        String tokenEndpoint = config.getString("token_endpoint");

        List<String> rootCertSubjects = new ArrayList<>();
        if (config.has("rootCertSubjects")) {
            JSONArray roots = config.getJSONArray("rootCertSubjects");
            for (int i = 0; i < roots.length(); i++) {
                rootCertSubjects.add(roots.getString(i));
            }
        }

        Scanner scanner = new Scanner(System.in);
        System.out.println("Enter character: (C)ertificateStore or (F)ile or (E)ntraID or (I)nteractive EntraID");
        String input = scanner.nextLine().toLowerCase();

        X509Certificate certificate = null;
        PrivateKey privateKey = null;
        List<com.nimbusds.jose.util.Base64> x5c = new ArrayList<>();
        String email = "";
        String entraid = "";

        switch (input) {
            case "f" -> {
	            // Zertifikat laden
	            KeyStore keyStore = KeyStore.getInstance("PKCS12");
	            try (FileInputStream fis = new FileInputStream("Andreas_Orzelski_Chain.pfx")) {
	                keyStore.load(fis, "i40".toCharArray());
	            }
	       
	            String alias = keyStore.aliases().nextElement();
	            certificate = (X509Certificate) keyStore.getCertificate(alias);
	            privateKey = (PrivateKey) keyStore.getKey(alias, "i40".toCharArray());
	 
	            // x5c-Kette extrahieren
	            java.security.cert.Certificate[] certChain = keyStore.getCertificateChain(alias);
	            for (java.security.cert.Certificate cert : certChain) {
	            	x5c.add(com.nimbusds.jose.util.Base64.encode(cert.getEncoded()));
	            }

				X500Name x500name = new JcaX509CertificateHolder(certificate).getSubject();
				RDN emailRDN = x500name.getRDNs(BCStyle.EmailAddress)[0];
				email = IETFUtils.valueToString(emailRDN.getFirst().getValue());
	                  
	            if (email == null) {
	                System.out.println("Keine E-Mail im Zertifikat gefunden.");
	                return;
	            }
            }
            case "e" -> {
                System.out.println("Entra ID?");
                entraid = scanner.nextLine();
            }
            case "i" -> {
                /*
                PublicClientApplication app = PublicClientApplication
                    .builder(clientId)
                    .authority("https://login.microsoftonline.com/common/")
                    .build();

                // 2) Interaktiven Request (Redirect URI + Scopes) aufsetzen
                InteractiveRequestParameters parameters = InteractiveRequestParameters
                    .builder(new URI("http://localhost"))
                    .scopes(scopes)
                    .build();

                // 3) Token abholen (blockierend)
                IAuthenticationResult result = app.acquireToken(parameters).join();

                // 4) ID-Token und User-Identität ausgeben
                System.out.println("ID Token:       " + result.idToken());
                */
                String tenantId = "common";
                String authority = "https://login.microsoftonline.com/" + tenantId;

                PublicClientApplication app = PublicClientApplication.builder(clientId)
                    .authority(authority)
                    .build();

                Set<String> scopes2 = new HashSet<>();

                DeviceCodeFlowParameters parameters = DeviceCodeFlowParameters.builder(
                        scopes2,
                        (DeviceCode deviceCode) -> {
                            System.out.println(deviceCode.message()); // zeigt URL + Code
                        })
                    .build();

                IAuthenticationResult result = app.acquireToken(parameters).get();

                System.out.println("Access Token: " + result.accessToken());
                System.out.println("ID Token: " + result.idToken());
                System.out.println("Username: " + result.account().username());
                    }
        }

        // Create JWT
        Instant now = Instant.now();
        JWTClaimsSet.Builder claimsBuilder = new JWTClaimsSet.Builder()
                .jwtID(UUID.randomUUID().toString())
                .subject("client.jwt")
	            .issuer("client.jwt")
                .issueTime(Date.from(now))
                .expirationTime(Date.from(now.plusSeconds(60)));

        if (entraid.isEmpty()) {
            claimsBuilder.claim("email", email);
        } else {
            claimsBuilder.claim("entraid", entraid);
        }

        JWTClaimsSet claims = claimsBuilder.build();

        JWSHeader.Builder headerBuilder = new JWSHeader.Builder(JWSAlgorithm.RS256)
                .type(JOSEObjectType.JWT);

        if (x5c != null) {
            headerBuilder.x509CertChain(x5c);
        }

        SignedJWT signedJWT;
        if (entraid.isEmpty()) {
            JWSSigner signer = new RSASSASigner((RSAPrivateKey) privateKey);
            signedJWT = new SignedJWT(headerBuilder.build(), claims);
            signedJWT.sign(signer);
        } else {
            byte[] secret = entraid.getBytes(StandardCharsets.UTF_8);
            JWSSigner signer = new MACSigner(secret);
            signedJWT = new SignedJWT(new JWSHeader(JWSAlgorithm.HS256), claims);
            signedJWT.sign(signer);
        }

        String jwt = signedJWT.serialize();

        // Request token
        HttpRequest tokenRequest = HttpRequest.newBuilder()
                .uri(URI.create(tokenEndpoint))
                .header("Content-Type", "application/x-www-form-urlencoded")
                .POST(HttpRequest.BodyPublishers.ofString(
                        "grant_type=client_credentials&scope=factoryx" +
                                "&client_assertion_type=urn:ietf:params:oauth:client-assertion-type:jwt-bearer" +
                                "&client_assertion=" + jwt))
                .build();

        HttpResponse<String> tokenResponse = client.send(tokenRequest, HttpResponse.BodyHandlers.ofString());
        System.out.println("Access Token Response: " + tokenResponse.body());
    }
}
