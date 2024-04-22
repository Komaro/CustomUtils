/**
 * 스프레드 시트 반환
 * Get fileName spread sheet
 * @param {string} fileName
 * @returns {SpreadsheetApp.Spreadsheet}
 */
function getSpreadSheet(fileName) {
    var fileIter = DriveApp.getFilesByName(fileName)
    var file = fileIter.hasNext() ? fileIter.next() : null
    if (file == null) {
        console.error('[spreadSheetManager.getSpreadSheet] file is Null')
        return null
    }

    var sheet = SpreadsheetApp.open(file)
    if (sheet == null) {
        console.error('[spreadSheetManager.getSpreadSheet] sheet is Null or Not SpreadSheet File')
        return null
    }

    return sheet
}
