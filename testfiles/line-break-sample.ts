// This is a long comment with word breaks. It should break cleanly no matter what the css settings are.
// This is a long comment that may break:  012345678901234567890123456789012345678901234567890123456789
// 0123456789012345678901234567890123456789012345678901234567890123456789012345678901234567890123456789
var n1:number = 569 // global variable
 
class Person { 
   name:string //class variable
 
   static sval = 10 //static property

   static aVeryLongPropertyNameThatMayOrMayNotWrapDependingOnSettingsBecauseHtmlIsTricky.
  
   printSomethingThenPrintSomethingElseBecauseThisIsReallyLong():void { 
      var message0:string = 'ReallyLongStringLiteralThatShouldBreakButProbablyWontBecauseLiteHtmlIs Lame!' //local variable
      var message1:string = 'Really Long String Literal That Should Break Because LiteHtml Is Lame!' //local variable
   } 
}

for(var counter:number = 1; counter<10; counter++){
    console.log("for loop executed : " + counter)
}