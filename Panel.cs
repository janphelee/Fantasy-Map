using Godot;

public class Panel : Godot.Panel
{
    [Export] private Texture texture = null;

    public override void _Draw()
    {
        //base._Draw();
        var rr = GetParentAreaSize();
        var ts = texture.GetSize();
        DrawTexture(texture, (rr - ts) / 2);
    }

    private void _on_Button_button_down()
    {
        var pp = GetNode<Label>("Label");
        pp.Text = "范文芳发射点发顺丰";
    }

}
