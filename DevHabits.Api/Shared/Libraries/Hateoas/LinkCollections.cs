using System.Collections;
using System.Collections.ObjectModel;

namespace DevHabits.Api.Shared.Libraries.Hateoas;

public class LinkCollections : IEnumerable<KeyValuePair<string, LinkDto>> {
    private readonly Dictionary<string, LinkDto> _links = [];
    public ReadOnlyDictionary<string, LinkDto> Links => _links.AsReadOnly();

    // Implement IEnumerable
    public IEnumerator<KeyValuePair<string, LinkDto>> GetEnumerator() => _links.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => _links.GetEnumerator();

    public bool Remove(string rel) => _links.Remove(rel);

    #region Add

    public LinkCollections Add(string rel, LinkDto link) {
        _links.Add(rel, link);
        return this;
    }

    public LinkCollections Add(string rel, string href, string method) =>
        Add(rel, new LinkDto { Href = href, Method = method });

    public LinkCollections TryAdd(string rel, LinkDto link) {
        _links.TryAdd(rel, link);
        return this;
    }

    public LinkCollections TryAdd(string rel, string href, string method) =>
        TryAdd(rel, new LinkDto { Href = href, Method = method });

    #endregion

    #region Add Self

    public LinkCollections AddSelf(LinkDto link) => Add("self", link);
    public LinkCollections AddSelf(string href) => AddSelf(LinkDto.CreateGet(href));

    public LinkCollections TryAddSelf(LinkDto link) => TryAdd("self", link);
    public LinkCollections TryAddSelf(string href) => TryAddSelf(LinkDto.CreateGet(href));

    #endregion

    #region Add Update

    public LinkCollections AddUpdate(LinkDto link) => Add("update", link);
    public LinkCollections AddUpdate(string href) => AddUpdate(LinkDto.CreatePut(href));

    public LinkCollections TryAddUpdate(LinkDto link) => TryAdd("update", link);
    public LinkCollections TryAddUpdate(string href) => TryAddUpdate(LinkDto.CreatePut(href));

    #endregion

    #region Add Delete

    public LinkCollections AddDelete(LinkDto link) => Add("delete", link);

    public LinkCollections AddDelete(string href) => AddDelete(LinkDto.CreateDelete(href));

    public LinkCollections TryAddDelete(LinkDto link) => TryAdd("delete", link);

    public LinkCollections TryAddDelete(string href) => TryAddDelete(LinkDto.CreateDelete(href));

    #endregion

    #region Add Create

    public LinkCollections AddCreate(LinkDto link) => Add("create", link);
    public LinkCollections AddCreate(string href) => AddCreate(LinkDto.CreatePost(href));

    public LinkCollections TryAddCreate(LinkDto link) => TryAdd("create", link);
    public LinkCollections TryAddCreate(string href) => TryAddCreate(LinkDto.CreatePost(href));

    #endregion
}
