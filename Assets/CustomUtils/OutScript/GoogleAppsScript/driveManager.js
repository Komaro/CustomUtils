/**
 * 구글 드라이브의 루트에서 폴더명이 folderName인 폴더를 검색해서 반환
 * 폴더가 없고 isCreate 가 true일 경우 새로 생성하여 반환
 * Get folderName Google Drive Folder
 * @param {string} folderName
 * @param {boolean} isCreate
 * @returns {DriveApp.Folder}
 */
function getDriveFolder(folderName, isCreate=true) {
    var folderIter = DriveApp.getFoldersByName(folderName)
    var folder = folderIter.hasNext() ? folderIter.next() : null
    if (folder == null && isCreate) {
        folder = createDriveFolder(folderName)
    }
    return folder
}

/**
 * 루트 폴더에 folderName으로 된 폴더를 생성 후 반환
 * Create folderName Google Drive Folder to Root Path
 * @param {string} folderName
 * @returns {DriveApp.Folder}
 */
function createDriveFolder(folderName) {
    return DriveApp.createFolder(folderName)
}

/**
 * targetFolder 하위에 이름이 folderName으로 된 폴더를 생성 후 반환
 * Create folderName Google DriveFolder to targetFolder
 * @param {DriveApp.Folder} targetFolder
 * @param {string} folderName
 * @returns {DriveApp.Folder}
 */
function createDriveFolderToTarget(targetFolder, folderName) {
    return targetFolder.createFolder(folderName)
}

/**
 * targetFolder의 하위에 fileName으로 된 이름의 파일을 생성.
 * 파일은 content로 이루어져 있으며 만약 동일한 이름의 파일이 이미 존재하고 isOverwirte가 true임면 동일한 이름의 파일을 삭제하고 새로 파일을 생성한다.
 * Create Google Drive File
 * @param {DriveApp.Folder targetFolder
 * @param {string} fileName
 * @param {string} content
 * @param {boolean} isOverwrite
 * @returns {DriveApp.File}
 */
function createDriveFile(targetFolder, fileName, content, isOverwrite=true) {
    checkDriveStorageSize()

    var fileIter = targetFolder.getFilesByName(fileName)

    var file = fileIter.hasNext() ? fileIter.next() : null
    if (file != null && isOverwrite) {
        file.setTrashed(true)
    }

    return targetFolder.createFile(fileName, content)
}

/**
 * targetFolder에 있는 파일이 maxLeftCount보다 많을 경우 그 차이많큼 오래된 파일을 삭제
 * Remove old file to targetFolder
 * @param {DriveApp.Folder} targetFolder
 * @param {int} maxLeftCount
 */
function removeOldFile(targetFolder, maxLeftCount=1) {
    var fileIter = targetFolder.getFiles()
    var count = 1
    var fileList = []
    while(fileIter.hasNext()) {
        fileList.push(fileIter.next())
        count++
    }

    if (count > maxLeftCount) {
        // 수정된 날자가 옛날인 순서로 정렬
        fileList.sort((x, y) => x.getLastUpdated().getTime() - y.getLastUpdated().getTime())

        var removeCount = count - maxLeftCount
        for(var i = 0; i < removeCount; i++) {
            if (fileList.length <= i) {
                break
            }

            var removeFile = fileList[i]
            if (removeFile != null) {
                console.log('[removeOldFile] Remove Old File || ' + removeFile.getName())
                removeFile.setTrashed(true)
            }
        }
    }

    checkDriveStorageSize()
}

/**
 * targetFolder에 있는 폴더가 maxLeftCount보다 많을 경우 그 차이많큼 오래된 폴더를 삭제
 * Remove old folder to targetFolder
 * @param {DriveApp.Folder} targetFolder
 * @param {int} maxLeftCount
 */
function removeOldFolder(targetFolder, maxLeftCount = 1) {
    var folderIter = targetFolder.getFolders()
    var count = 1
    var folderList = []
    while(folderIter.hasNext()) {
        folderList.push(folderIter.next())
        count++
    }

    if (count > maxLeftCount) {
        // 수정된 날자가 옛날인 순서로 정렬
        folderList.sort((x, y) => x.getLastUpdated().getTime() - y.getLastUpdated().getTime())

        var removeCount = count - maxLeftCount
        for(var i = 0; i < removeCount; i++) {
            if (folderList.length <= i) {
                break
            }

            var removeFolder = folderList[i]
            if(removeFolder != null) {
                console.log('[removeOldFolder] Remove Old Folder || ' + removeFolder.getName())
                removeFolder.setTrashed(true)
            }
        }
    }

    checkDriveStorageSize()
}

/**
 * 드라이브의 현재 사용량을 확인해서 maxPercentage 이상 사용중이면 휴지통을 정리함
 * Check Google Drive Garbage Storage
 * @param {folat} maxPercentage
 */
function checkDriveStorageSize(maxPercentage = 0.9) {
    var about = Drive.About.get()
    if (about != null) {
        console.log('[checkDriveStorageSize] ' + about.quotaBytesUsedAggregate + ' / ' + about.quotaBytesTotal * maxPercentage)
        if (about.quotaBytesUsedAggregate >= about.quotaBytesTotal * maxPercentage) {
            emptyTrash()
        } else {
            console.log('[checkDriveStorageSize] Enough Storage')
        }
    }
}

/**
 * 휴지통 정리
 * Clear Google Drive Trash Storage
 */
function emptyTrash() {
    try {
        Drive.Files.emptyTrash()
        console.log('[emptyTrash] Empty Trash is Done')
    } catch(error) {
        console.error("[emptyTrash] " + error)
    }
}