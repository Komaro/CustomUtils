/**
 * 현재 활성화중인 Sheet를 XML로 변환
 * Current active sheet convert to XML
 * @returns {string}
 */
function createXml() {
    var sheet = SpreadsheetApp.getActiveSheet()

    console.log(sheet.getName() + ' || ' + sheet.getDataRange() )

    var data = sheet.getDataRange().getValues()

    if (data[0][0] == '') {
        console.log('Already xml sheet')
        return
    }

    var maxColumn = data[0].length;
    var root = XmlService.createElement(data[0][0])
    for (var row = 1; row < data.length; row++) {
        var element = XmlService.createElement(data[row][0])
        for(var column = 1; column < maxColumn; column++) {
            console.log('[createXml] ' + data[0][column] + ' || ' + data[row][column])
            element.setAttribute(data[0][column], data[row][column])
        }
        root.addContent(element)
    }

    for(var x = 0; x < root.getAllContent().length; x++) {
        var logText = '[creatXml] '
        logText += root.getContent(x)
    }

    var document = XmlService.createDocument(root)

    var xml = XmlService.getPrettyFormat().format(document)
    return xml
}
