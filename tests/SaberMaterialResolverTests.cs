using System;
using OldSaber;
namespace UnityEngine
{
    public class Object {public static implicit operator bool(Object o)=>o!=null;}
    public class Shader:Object {public string name;public bool isSupported=true;}
    public class Material:Object
    {
        public string name;public Shader shader;public bool color=true,reflection;
        public bool HasProperty(string property)=>property=="_Color"?color:property=="_ReflectionTex"&&reflection;
    }
}
public static class ResolverTests
{
    static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
    static UnityEngine.Material M(string name,string shader)=>new UnityEngine.Material{name=name,shader=new UnityEngine.Shader{name=shader}};
    public static void Main()
    {
        Check(!SaberMaterialResolver.RendersPart("Cylinder"),"Ring geometry must not render");
        Check(SaberMaterialResolver.RendersPart("Blade") && SaberMaterialResolver.RendersPart("Handle"),"Blade and handle must remain");
        Check(!SaberMaterialResolver.UsesHandleMaterial("Ornament") && SaberMaterialResolver.UsesHandleMaterial("Handle"),"Ornament must keep its player-color glow material while the handle stays neutral");
        Check(!SaberMaterialResolver.UsesHandleMaterial("Blade"),"Blade must keep its glow material");
        var blade=M("SaberBlade","Modern/Glowing");var handle=M("SaberHandle","Modern/Lit");
        var fake=M("SaberBladeFake","Custom/FakeGlow");var mirror=M("Mirror","Modern/Mirror");mirror.reflection=true;
        var unsupported=M("SaberBladeUnsupported","Missing");unsupported.shader.isSupported=false;
        var candidates=new[]{fake,mirror,unsupported,null,handle,blade};
        Check(SaberMaterialResolver.Select(candidates,false)==blade,"Missing legacy shader must use loaded blade material");
        Check(SaberMaterialResolver.Select(candidates,true)==handle,"Handle must use handle material");
        Check(SaberMaterialResolver.Select(new[]{fake,mirror,unsupported},false)==null,"Incompatible material must be rejected");
        Check(SaberMaterialResolver.Select(new[]{handle},false)==handle,"Usable generic fallback rejected");
        Console.WriteLine("PASS: saber material fallback selects loaded blade/handle materials and rejects mirrors, fake glows and unsupported shaders.");
    }
}
