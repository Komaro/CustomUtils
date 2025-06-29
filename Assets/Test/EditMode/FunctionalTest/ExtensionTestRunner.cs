using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;

[Category(TestConstants.Category.FUNCTIONAL)]
public class ExtensionTestRunner {

    [TestCase("C:/Users/KAKA/Downloads/TestText", '/')]
    [TestCase("Param.Field.Value", '.')]
    public void GetAfterCharTest(string content, char matchChar) {
        var matchCaseList = CreateAllMatchCaseList(content, matchChar);
        foreach (var getCase in GetAllGetAfterCase(content, matchChar)) {
            matchCaseList.AssertionContains(getCase);
        }
        
        TestContext.WriteLine();
        Logger.TraceLog($"Pass {MethodBase.GetCurrentMethod()?.GetCleanFullName() ?? string.Empty}");
    }

    [TestCase("C:/Users/KAKA/Downloads/TestText", "Downloads")]
    [TestCase("Param.Field.Value", "Field")]
    public void GetAfterStringTest(string content, string matchContent) {
        var matchCaseList = CreateAllMatchCaseList(content, matchContent);
        foreach (var getCase in GetAllGetAfterCase(content, matchContent)) {
            matchCaseList.AssertionContains(getCase);
        }

        TestContext.WriteLine();
        Logger.TraceLog($"Pass {MethodBase.GetCurrentMethod()?.GetCleanFullName() ?? string.Empty}");
    }

    [TestCase("C:/Users/KAKA/Downloads/TestText", '/')]
    [TestCase("Param.Field.Value", '.')]
    public void GetBeforeCharTest(string content, char matchChar) {
        var matchCaseList = CreateAllMatchCaseList(content, matchChar);
        foreach (var getCase in GetAllGetBeforeCase(content, matchChar)) {
            matchCaseList.AssertionContains(getCase);
        }

        TestContext.WriteLine();
        Logger.TraceLog($"Pass {MethodBase.GetCurrentMethod()?.GetCleanFullName() ?? string.Empty}");
    }
    
    [TestCase("C:/Users/KAKA/Downloads/TestText", "Downloads")]
    [TestCase("Param.Field.Value", "Field")]
    public void GetBeforeStringTest(string content, string matchContent) {
        var matchCaseList = CreateAllMatchCaseList(content, matchContent);
        foreach (var getCase in GetAllGetBeforeCase(content, matchContent)) {
            matchCaseList.AssertionContains(getCase);
        }

        TestContext.WriteLine();
        Logger.TraceLog($"Pass {MethodBase.GetCurrentMethod()?.GetCleanFullName() ?? string.Empty}");
    }

    private List<string> CreateAllMatchCaseList(string content, char matchChar) => CreateAllMatchCaseList(content, matchChar.ToString());

    private List<string> CreateAllMatchCaseList(string content, string matchContent) {
        var firstIndex = content.IndexOf(matchContent, StringComparison.Ordinal);
        Assert.IsFalse(firstIndex < 0);
        
        var lastIndex = content.LastIndexOf(matchContent, StringComparison.Ordinal);
        Assert.IsFalse(lastIndex < 0);
        
        var matchCaseList = new List<string> {
            content[firstIndex..], content[(firstIndex + matchContent.Length)..], content[lastIndex..], content[(lastIndex + matchContent.Length)..],
            content[..firstIndex], content[..(firstIndex + matchContent.Length)], content[..lastIndex], content[..(lastIndex + matchContent.Length)],
        };
        
        Assert.IsNotEmpty(matchCaseList);
        Logger.TraceLog(matchCaseList.ToStringCollection(", "));
        return matchCaseList;
    }

    private IEnumerable<string> GetAllGetAfterCase(string content, char matchChar) => GetAllGetAfterCase(content, matchChar.ToString());

    private IEnumerable<string> GetAllGetAfterCase(string content, string stringContent) {
        yield return content.GetAfter(stringContent);
        yield return content.GetAfter(stringContent, true);
        yield return content.GetAfter(stringContent);
        yield return content.GetAfter(stringContent, true);
        yield return content.GetAfterFirst(stringContent);
        yield return content.GetAfterFirst(stringContent, true);
        yield return content.GetAfterFirst(stringContent);
        yield return content.GetAfterFirst(stringContent, true);
    }

    private IEnumerable<string> GetAllGetBeforeCase(string content, char matchChar) => GetAllGetBeforeCase(content, matchChar.ToString());

    private IEnumerable<string> GetAllGetBeforeCase(string content, string matchContent) {
        yield return content.GetBefore(matchContent);
        yield return content.GetBefore(matchContent, true);
        yield return content.GetBefore(matchContent);
        yield return content.GetBefore(matchContent, true);
        yield return content.GetBeforeFirst(matchContent);
        yield return content.GetBeforeFirst(matchContent, true);
        yield return content.GetBeforeFirst(matchContent);
        yield return content.GetBeforeFirst(matchContent, true);
    }
}