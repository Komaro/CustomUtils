set APP_PATH=%1
set KEY_STORE_PATH=%2
set KEY_STORE_PASS=%3
set KEY_STORE_ALIAS=%4
set COPY_PATH=%5
set APKSIGNER_PATH=%6

set APP_PATH_ZA=%APP_PATH:.apk=_za.apk%
set APP_PATH_SIGNED=%APP_PATH:.apk=_signed.apk%

Echo Start APK Signing

java -jar %APKSIGNER_PATH% sign -v --out %APP_PATH_SIGNED% --ks %KEY_STORE_PATH% --ks-pass %KEY_STORE_PASS% --ks-key-alias %KEY_STORE_ALIAS %APP_PATH_ZA%

Echo Copy APK files
