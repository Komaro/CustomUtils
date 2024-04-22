var COMMA_REGEX = /\B(?=(\d{3})+(?!\d))/g;

var KB = 'KB'
var MB = 'MB'
var GB = 'GB'
var TB = 'TB'
var PB = 'PB'

/**
 * 현재 시간을 연속된 String 숫자집합으로 반환
 * Current time convert to string digit
 * @param {string} prefix
 * @param {string} suffix
 * @returns {string}
 */
function createDateString(prefix = '', suffix = '') {
    var date = new Date()
    return String(prefix + date.getFullYear()
        + createZeroDigit((date.getMonth() + 1), 2)
        + createZeroDigit(date.getDate(), 2)
        + createZeroDigit(date.getHours(), 2)
        + createZeroDigit(date.getMinutes(), 2)
        + createZeroDigit(date.getSeconds(), 2) + suffix).toString()
}

/**
 * 입력한 number와 digit를 비교해 비는 공간만큼 0을 넣어서 String 반환
 * Create zero digit
 * @param {int} number
 * @param {int} digit
 * @returns {string}
 */
function createZeroDigit(number, digit) {
    return number.toLocaleString('en-US', { minimumIntegerDigits: digit, useGrouping: false })
}

// 줄바꿈 문자를 동일하게  전부 수정
function removeNewLine(text) {
    return text.replace(/\r\n|\n/g, "\\n")
}

/**
 * 숫자에 , 추가
 * 999999 => 999,999
 * Create comma digit to string
 * @param {int} num
 */
function createCommaDigit(num) {
    return num.toString().replace(COMMA_REGEX, ',');
}

/**
 * Memory Unit 출력
 * Create memory unity digit to string
 * @param {int} num
 */
function createMemoryUnitDigit(num) {
    var commaDigit = createCommaDigit(num)
    if (num === '') {
        return ''
    }

    var splitDigit = commaDigit.split(',')
    var memoryUnitDgit = splitDigit[0]
    var unit = ''
    switch(splitDigit.length) {
        case 1:
            // K
            unit = KB
            break
        case 2:
            // M
            unit = MB
            break
        case 3:
            // G
            unit = GB
            break
        case 4:
            // T
            unit = TB
            break
        case 5:
            // P
            unit = PB
            break
    }

    if (unit === '') {
        console.error('[createMemoryUnitDigit] Overflow digit')
        return ''
    }

    return memoryUnitDgit + unit
}