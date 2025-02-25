using System;
using System.IO;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util;
using Google.Apis.Util.Store;

public class GoogleCredentialService {

    public GoogleCredential GetGoogleCredentialFromFile(string path) => GoogleCredential.FromFile(path.AutoSwitchExtension(Constants.Extension.JSON));
    public async Task<GoogleCredential> GetGoogleCredentialFromFileAsync(string path) => await GoogleCredential.FromFileAsync(path.AutoSwitchExtension(Constants.Extension.JSON), CreateCancellationToken());

    public UserCredential GetUserCredential(string userId, params string[] apiScopes) => GetUserCredentialAsync(userId, apiScopes).Result;

    public async Task<UserCredential> GetUserCredentialAsync(string userId, params string[] apiScopes) {
        var credentialPath = $"{Constants.Path.PROJECT_PATH}/GoogleApi/Credentials";
        using (var stream = new FileStream($"{credentialPath}/{userId}_credentials.json", FileMode.Open, FileAccess.Read)) {
            var secrets = await GoogleClientSecrets.FromStreamAsync(stream);
            if (secrets == null) {
                throw new InvalidCredentialException($"{nameof(secrets)} is null");
            }
            
            var userCredential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                secrets.Secrets,
                apiScopes,
                userId,
                CreateCancellationToken(),
                new FileDataStore(credentialPath, true)
            );

            if (userCredential == null) {
                throw new InvalidCredentialException($"{nameof(userCredential)} is null");
            }

            if (userCredential.Token.IsExpired(SystemClock.Default)) {
                await GoogleWebAuthorizationBroker.ReauthorizeAsync(userCredential, CreateCancellationToken());
            }
            
            return userCredential;
        }
    }

    public CancellationToken CreateCancellationToken(double delaySeconds = 20d) => new CancellationTokenSource(TimeSpan.FromSeconds(delaySeconds)).Token;
}