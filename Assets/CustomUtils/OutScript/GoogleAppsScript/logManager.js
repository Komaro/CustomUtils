// TODO. 라이브러리 형식으로 호출 시 Caller를 정상적으로 찾을 수 없음.
// 로그 출력
function log(text) {
    var caller = arguments.callee.caller
    console.log('[' + getCallerScript() + '.' + caller.name + '] ' + text)
}

// 에러 출력
function error(text) {
    var caller = arguments.callee.caller
    console.error('[' + getCallerScript() + '.' + caller.name + '] ' + text)
}

// 로그를 호출한 Caller function의 Script name을 출력
function getCallerScript() {
    var stack = new Error().stack || ''; stack = stack.split('\n').map(
        function (line) {
            return line.trim();
        }
    );

    console.log(stack)
    var spliceStack = stack.splice(stack[0] == 'Error' ? 2 : 1)
    console.log(spliceStack)
    var returnText = spliceStack[1].split(' ')[2].replace('(', '').replace(')', '').split(':')[0]
    return returnText
}