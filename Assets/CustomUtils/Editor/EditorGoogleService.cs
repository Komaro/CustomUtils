using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Google.Apis.Auth.OAuth2;
using UnityEditor;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Google.Apis.Util;
using Google.Apis.Util.Store;
using UnityEngine;
using File = Google.Apis.Drive.v3.Data.File;

public class EditorGoogleService : EditorWindow {

	private string _eMail;
    private bool _isDebug = false;
    private string _debugDriveQuery = string.Empty;

    readonly string[] API_SCOPES = { 
        DriveService.Scope.Drive,
        SheetsService.Scope.SpreadsheetsReadonly 
    };
    
    private const string USER = "user";
	private const string L10_DRIVE_QUERY = "name contains 'Some_Name_Prefix' and mimeType = 'application/vnd.google-apps.spreadsheet'";
	private const string TEST_L10_DRIVE_QUERY = "not name contains '\\^' and not name contains '@' and not name contains '*' and not name contains '#' and name contains 'Some Text' and mimeType = 'application/vnd.google-apps.spreadsheet'";
	private const string DRIVE_FIELDS = "files(id, name, owners)";
	private const string GOOGLE_FOLDER_MIME_TYPE = "mimeType = 'application/vnd.google-apps.folder'";

	// TODO. Fix Select Target Folder
	private const string GOOGLE = "Google";
	private const string CREDENTIAL_JSON = "credentials.json";

	private double CREDENTIAL_REQUEST_TIME_OUT = 25d;

	private UserCredential _cacheCredential;
	
	[MenuItem("Service/Google Service")]
	public static void OpenWindow() {
		var window = GetWindow<EditorGoogleService>("GoogleService");
		window.Show();
	}

	private void OnEnable() => _cacheCredential = null;
	private void OnDisable() => _cacheCredential = null;

	private void OnGUI() {
		_isDebug = GUILayout.Toggle(_isDebug, "Debug");
		if (_isDebug) {
			GUILayout.Label(" === Debug (Test) === ");
			
			GUILayout.Label("Drive Query");
			_debugDriveQuery = GUILayout.TextArea(_debugDriveQuery);

			GUILayout.Label("E-Mail");
			_eMail = GUILayout.TextArea(_eMail);
			
			GUILayout.Label(" ============= ");
			GUILayout.Space(10);
		}
		
		// TODO. Create Progress
		
		GUILayout.Space(30);
	}

	private void ShowCheckDialogue(string title, string message, string okText, string cancelText, Action ok = null, Action cancel = null) {
		if (EditorUtility.DisplayDialog(title, message, okText, cancelText)) {
			ok?.Invoke();
		} else {
			cancel?.Invoke();
		}
	}

	private DriveService ConnectDriveService() {
		CreateCredential();
		if (_cacheCredential == null) {
			Logger.TraceError($"{nameof(_cacheCredential)} is Null");
			return null;
		}
		
		var service = new DriveService(new BaseClientService.Initializer {
			HttpClientInitializer = _cacheCredential,
			ApplicationName = string.Empty
		});

		return service;
	}

	private SheetsService ConnectSheetService() {
		CreateCredential();
		if (_cacheCredential == null) {
			Logger.TraceError($"{nameof(_cacheCredential)} is Null");
			return null;
		}
		
		var service = new SheetsService(new BaseClientService.Initializer {
			HttpClientInitializer = _cacheCredential,
			ApplicationName = string.Empty
		});

		return service;
	}
	
	private void CreateCredential() {
		if (_cacheCredential != null && _cacheCredential.Token.IsExpired(SystemClock.Default) == false) {
			Logger.TraceLog("Token is Not Expired", Color.green);
			return; 
		}
		
		try {
			SystemUtil.CreateDirectory(GetGooglePath());
			using var stream = new FileStream(GetGooglePath(CREDENTIAL_JSON), FileMode.Open, FileAccess.Read);
			var tokenPath = GetGooglePath();
			var cancellationToken = CreateCancellationToken();
			_cacheCredential = GoogleWebAuthorizationBroker.AuthorizeAsync(
				GoogleClientSecrets.FromStream(stream).Secrets,
				API_SCOPES,
				USER,
				cancellationToken,
				new FileDataStore(tokenPath, true)).Result;
		} catch (Exception ex) {
			EditorUtility.DisplayDialog("GoogleService", "Time Out", "OK");
			Logger.TraceError(ex.Message);
		}
	}

	private CancellationToken CreateCancellationToken() {
		var cancellationTokenSource = new CancellationTokenSource();
		cancellationTokenSource.CancelAfter(TimeSpan.FromSeconds(CREDENTIAL_REQUEST_TIME_OUT));
		return cancellationTokenSource.Token;
	}

	private FileList GetSpreadSheetFileList(DriveService service) {
		var request = service.Files.List();
		request.Q = string.IsNullOrEmpty(_debugDriveQuery) ? L10_DRIVE_QUERY : _debugDriveQuery;
		request.Fields = DRIVE_FIELDS;
		
		var fileList = request.Execute();
		var builder = new StringBuilder().AppendLine();
		if (fileList != null && fileList.Files.Count > 0) {
			foreach (var file in fileList.Files) {
				var email = file.Owners.FirstOrDefault()?.EmailAddress;
				if (email?.Contains(_eMail) ?? false) {
					builder.AppendLine($"{file.Name} || {file.Id} || {email}");	
				}
			}
			Logger.TraceLog(builder.ToString(), Color.cyan);
		}

		return fileList;
	}

	private List<File> GetSheetList(DriveService service) => GetSpreadSheetFileList(service)?.Files.Where(x => x.Owners.FirstOrDefault()?.EmailAddress.Contains(_eMail) ?? false).ToList();
	private string GetGooglePath(string fileName = "") => string.IsNullOrEmpty(fileName) ? $"{Application.dataPath}/{GOOGLE}" : $"{Application.dataPath}/{GOOGLE}/{fileName}";
}
