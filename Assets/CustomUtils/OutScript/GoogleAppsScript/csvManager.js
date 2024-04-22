var SEPERATOR = "┃";
var MAX_COL = 16;

/**
 * L10 최종 출력본 반환. checkFunction boolean 반환 필수
 * create L10 Text
 * @param {string} fileName
 * @params {function} checkFunction
 * @returns {string}
 */
function createL10Text(fileName, checkFunction) {
    var spreadSheet = getSpreadSheet(fileName)
    console.log('[createL10TextFile] SpreadSheet Name || ' + spreadSheet.getName())

    var sheetList = spreadSheet.getSheets()
    var csv = ''
    for (var i = 0; i < sheetList.length; i++) {
        var sheet = sheetList[i]
        if (sheet == null) {
            continue
        }

        if (checkFunction(sheet.getName().charAt(0)) == false) {
            continue
        }

        if (sheet.getDataRange().getValues().join("") === "") {
            continue
        }

        csv += appendToCSV(sheet, csv, MAX_COL);
        csv += "\r\n";
    }

    return csv
}

/**
 * sheet를 SEPERATOR로 분할된 텍스트 파일 반환
 * @param {SpreadsheetApp.Sheet} sheet
 * @param {string} csv
 * @param {int} maxCol
 * @returns {string}
 */
function appendToCSV(sheet, csv, maxCol) {
    var activeRange = sheet.getDataRange();
    try {
        var data = activeRange.getValues();
        if (data.length > 1) {
            var csv = "";
            for (var row = 1; row < data.length; row++) {
                for (var col = 0; col < maxCol; col++) {
                    if (data[row][col] == null || data[row][col] == undefined) {
                        continue;
                    }
                    if (data[row][col].toString().indexOf(SEPERATOR) != -1) {
                        data[row][col] = "\"" + data[row][col] + "\"";
                    }

                    data[row][col] = data[row][col].toString().replace(/\r\n|\n/g, "\\n");
                }

                if (row < data.length) {
                    var append = data[row].splice(0, maxCol);
                    for (var i=0; i<maxCol; i++) {
                        if (append[i] == null || append[i] == undefined) {
                            append[i] == "";
                        }
                    }

                    if (isNullOrEmpty(append[0]) || isNullOrEmpty(append[1]))  {
                        continue;
                    }

                    csv += append.join(SEPERATOR) + "\r\n";
                }
            }
        }
        return csv;
    } catch(err) {
        Logger.log(err);
        Browser.msgBox(err);
    }
}

/**
 * targetSheet를 CSV 형식으로 반환
 * @param {SpreadsheetApp.Sheet} targetSheet
 * @returns {string}
 */
function createL10CSVFile(targetSheet) {
    var activeRange = targetSheet.getDataRange();
    try {
        var datas = activeRange.getValues();
        var csvFile = '';
        if (datas.length > 1) {
            var csv = "";
            for (var row = 0; row < datas.length; row++) {
                for (var col = 0; col < datas[row].length; col++) {
                    if (datas[row][col].toString().indexOf(",") != -1) {
                        datas[row][col] = "\"" + datas[row][col] + "\"";
                    }

                    // 줄바꿈 문자 제거
                    datas[row][col] = removeNewLine(datas[row][col].toString())
                }

                if (row < datas.length - 1) {
                    csv += datas[row].join(",") + "\r\n";
                } else {
                    csv += datas[row];
                }
            }
            csvFile = csv;
        }
        return csvFile;
    } catch (err) {
        console.error(err)
    }
}

// Null 체크
function isNullOrEmpty(str) {
    if (str == null || str == undefined || typeof(str) != "string") {
        return true;
    }

    return str === "";
}