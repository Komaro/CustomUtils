using System.Security.Authentication;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Services;

public class GoogleDriveService {

    public static DriveService Connect(ICredential credential) {
        if (credential == null) {
            throw new InvalidCredentialException($"{nameof(credential)} is null");
        }

        var service = new DriveService(new BaseClientService.Initializer {
            HttpClientInitializer = credential,
            ApplicationName = string.Empty
        });
 
        return service;
    }
}
