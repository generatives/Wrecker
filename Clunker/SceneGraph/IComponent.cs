namespace Clunker.SceneGraph
{
    public interface IComponent
    {
        Scene CurrentScene { get; }
        GameObject GameObject { get; }
        bool IsAlive { get; }
        bool IsActive { get; set; }
        string Name { get; set; }
    }
}