using System;
using OldschoolBeatSaber;
class Tests
{
 static void Main()
 {
  foreach(var count in new[]{0,1,5,6,566})foreach(var height in new[]{0f,40f,60f,72f,80f})foreach(var row in new[]{-1,0,3,565,999}){
   float p=TableScrollMath.Position(count,12,height,row),s=TableScrollMath.Step(count,12,height);
   if(float.IsNaN(p)||float.IsInfinity(p)||p<0||p>1||s<=0||s>1)throw new Exception("Invalid scroll position/step");
  }
  if(TableScrollMath.Position(5,12,60,0)!=1)throw new Exception("Exact-fit list must stay at top");
  if(TableScrollMath.Position(566,12,72,565)!=0)throw new Exception("Last custom song must be reachable");
  Console.WriteLine("PASS: empty, exact-fit and long lists have finite bounded scroll positions; last row remains reachable.");
 }
}
