#! /bin/sh

APP_PATH=$1
APP_NAME=$(basename ${APP_PATH})
KEY_STORE_PATH=$2
KEY_STORE_PASS=$3
KEY_STORE_ALIAS=$4
OUTPUT_PATH="${PWD}/${APP_NAME}"
COPY_PATH="$5/$APP_NAME"

#echo "$APP_PATH"
#echo "$APP_NAME"
#echo "$KEY_STORE_PATH"
#echo "$KEY_STORE_PASS"

#echo "$OUTPUT_PATH"
#echo "$COPY_PATH"

echo "Start AAB Signing"
jarsigner -verbose -sigalg SHA256withRSA -digestalg SHA-256 -keystore $KEY_STORE_PATH -storepass $KEY_STORE_PASS $APP_PATH $KEY_STORE_ALIAS

echo "Copy AAB file || ${OUTPUT_PATH} ==> ${COPY_PATH}"
cp $OUTPUT_PATH $COPY_PATH
rm -rf $APP_PATH