namespace DevHabits.Api.Helpers.Sort;

public interface ISortOptionsProvider<T> {
    SortOptions GetOptions();
}
