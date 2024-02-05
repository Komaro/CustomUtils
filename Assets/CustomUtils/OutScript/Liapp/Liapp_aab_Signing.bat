set APP_PATH=%1
set KEY_STORE_PATH=%2
set KEY_STORE_PASS=%3
set KEY_STORE_ALIAS=%4
set COPY_PATH=%5

Echo Start AAB Signing
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore %KEY_STORE_PATH% -storepass %KEY_STORE_PASS% %APP_PATH% %KEY_STORE_ALIAS

Echo Copy AAB File
copy %APP_PATH% %COPY_PATH%
del %APP_PATH%