using Godot;
using System;
using System.Linq;

public partial class TestGlbImport : SceneTree
{
    public override void _Initialize()
    {
        try
        {
            var packed = GD.Load<PackedScene>("res://temp/test_corvo_rigged.glb");
            if (packed == null)
            {
                GD.PrintErr("Failed to load packed scene!");
                Quit(1);
                return;
            }
            var node = packed.Instantiate<Node3D>();
            GD.Print("Loaded root node: ", node.Name);
            var animators = node.FindChildren("*", "AnimationPlayer", true, false).OfType<AnimationPlayer>().ToList();
            GD.Print("AnimationPlayers found: ", animators.Count);
            foreach (var anim in animators)
            {
                GD.Print("  Animator: ", anim.Name, " path: ", anim.GetPath());
                foreach (string a in anim.GetAnimationList())
                {
                    var animObj = anim.GetAnimation(a);
                    GD.Print("    Clip: ", a, " length: ", animObj?.Length, " tracks: ", animObj?.GetTrackCount());
                }
            }
            var meshes = node.FindChildren("*", "MeshInstance3D", true, false).OfType<MeshInstance3D>().ToList();
            GD.Print("MeshInstances: ", meshes.Count);
            foreach (var m in meshes)
            {
                GD.Print("  Mesh: ", m.Name, " surfaces: ", m.Mesh?.GetSurfaceCount());
                for (int s = 0; s < (m.Mesh?.GetSurfaceCount() ?? 0); s++)
                {
                    var mat = m.Mesh.SurfaceGetMaterial(s);
                    GD.Print("    Surface ", s, " mat: ", mat?.ResourceName);
                }
            }
            var heads = node.FindChildren("Head*", "Node3D", true, false).OfType<Node3D>().ToList();
            GD.Print("Head nodes found: ", heads.Count);
            foreach (var h in heads)
            {
                GD.Print("  Head: ", h.Name, " pos: ", h.Position, " (isMesh: ", h is MeshInstance3D, ")");
            }
            node.Free();
            GD.Print("=== TEST IMPORT SUCCESSFUL ===");
            Quit(0);
        }
        catch (Exception ex)
        {
            GD.PrintErr("Exception: ", ex);
            Quit(1);
        }
    }
}
