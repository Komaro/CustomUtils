#! /bin/sh

DEVELOPER_NAME=$1
IPA_PATH=$2
COPY_PATH=$3

#echo $DEVELOPER_NAME
#echo $IPA_PATH
#echo $COPY_PATH
#echo ""

APP_NAME=$(basename ${IPA_PATH} ".liapp")
IPA_NAME=$(basename ${APP_NAME} ".ipa")
APP_NAME=${IPA_NAME#LIAPP_with_}

#echo $APP_NAME
#echo $IPA_NAME
#echo ""

RESIGNING_NAME="${IPA_NAME}-resigned.ipa"
RESIGNING_PATH="${PWD}/${RESIGNING_NAME}"
COPY_PATH="${COPY_PATH}/${RESIGNING_NAME}"

#echo $SINGING_NAME
#echo $SIGNING_PATH
#echo $COPY_PATH
#echo ""

if [ -d $COPY_PATH ]
then
    rm -rf $COPY_PATH
fi

rm -rf $RESIGNING_NAME

unzip $IPA_PATH

if [ -d "Payload/$APP_NAME.app/Frameworks/UnityFramework.framework/" ]
then
    rm -rf "Payload/$APP_NAME.app/Frameworks/UnityFramework.framework/_CodeSignature/"
    codesign --preserve-metadata=entitlements -f -s  "$DEVELOPER_NAME"  "Payload/$APP_NAME.app/Frameworks/UnityFramework.framework/UnityFramework"

    echo "#### Code Sign Step for Unity complete ####"
fi

rm -rf "Payload/$APP_NAME.app/_CodeSignature/"

codesign --preserve-metadata=entitlements -f -s  "$DEVELOPER_NAME"  "Payload/$APP_NAME.app"

echo "#### Code Sign Step $APP_NAME complete ####"

#unity
zip -qr $RESIGNING_NAME Payload/ Symbols/ SwiftSupport/

rm -rf entitlements.xml
rm -rf Payload/
rm -rf Symbols/
rm -rf SwiftSupport/
rm -rf AppThinning.plist
rm -rf BCSymbolMaps/

echo "#### Code Sign Done ####"

cp $RESIGNING_PATH $COPY_PATH
rm $RESIGNING_PATH $IPA_PATH