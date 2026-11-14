using System;
using OldSaber;
public static class ClassicMenuLayoutTests
{
 public static void Main()
 {
  foreach(var s in new[]{new[]{160f,80f},new[]{120f,80f},new[]{240f,60f},new[]{80f,40f}})
  {float scale=ClassicMenuLayout.Fit(s[0],s[1]);if(160*scale>s[0] || 80*scale>s[1])throw new Exception("Menu exceeds canvas");}
  if(ClassicMenuLayout.Fit(0,0)!=1 || ClassicMenuLayout.Fit(float.NaN,80)!=1)throw new Exception("Uninitialized canvas collapses menu");
  if(ClassicMenuLayout.Fit(120,80)!=.75f)throw new Exception("Uniform proportions lost");
  Console.WriteLine("PASS: classic design canvas fits narrow/wide/short screens uniformly and survives uninitialized dimensions.");
 }
}
