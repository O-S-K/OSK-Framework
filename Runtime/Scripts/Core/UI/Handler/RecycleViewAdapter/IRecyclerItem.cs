namespace OSK
{
    /// <summary>
    /// Interface cho item view: dùng RecycleViewAdapter để bind data.
    /// Implement ở prefab item view.
    /// </summary>
    public interface IRecyclerItem<TModel>
    {
        void SetData(TModel model, int index);
        void Clear();               
    }
}
