#! /bin/sh

APP_PATH=$1
KEY_STORE_PATH=$2
KEY_STORE_PASS=$3
KEY_STORE_ALIAS=$4
COPY_PATH="$5/$APP_NAME"
ZIPALIGN_PATH=$6
APKSIGNER_PATH=$7

APP_PATH_ZA="${APP_PATH/.apk/_za.apk}"
APP_PATH_SIGNED="${APP_PATH/.apk/_signed.apk}"

#echo ""

#echo "$APP_PATH"
#echo "$KEY_STORE_PATH"
#echo "$KEY_STORE_PASS"
#echo "$ZIPALIGN_PATH"
#echo "$APKSIGNER_PATH"

#echo "$COPY_PATH"
#echo "${APP_PATH_ZA}"
#echo "${APP_PATH_SIGNED}"

echo "Start Zipalign"
$ZIPALIGN_PATH -f -v 4 ${APP_PATH} ${APP_PATH_ZA}

echo "Start apksigner"
$APKSIGNER_PATH sign -v --out ${APP_PATH_SIGNED} --ks ${KEY_STORE_PATH} --ks-key-alias ${KEY_STORE_ALIAS} --ks-pass pass:${KEY_STORE_PASS} ${APP_PATH_ZA}

echo "Copy Signed APK file || ${APP_PATH_SIGNED} ==> ${COPY_PATH}"
cp $APP_PATH_SIGNED $COPY_PATH
rm $APP_PATH
rm $APP_PATH_ZA
rm $APP_PATH_SIGNED
rm "${APP_PATH_SIGNED}.idsig"
