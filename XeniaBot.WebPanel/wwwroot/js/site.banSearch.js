const eSortBy = [
    'CreatedAt',
    'GuildId',
    'UserId',
    'Username'
];


function hookSelect() {
    const banSyncAvailableGuilds = JSON.parse(atob(document.getElementById('ban-search-root').getAttribute('data-available-guilds')));
    const selectElem = document.getElementById('select_ban-sync-search-requesting-guild');
    let selectedValue = selectElem
        .getAttribute('data-x-selected');
    if (selectedValue === '1') selectedValue = '0';
    console.log({
        selectedValue,
        banSyncAvailableGuilds,
        attrs: selectElem.attributes,
    })
    new TomSelect('#select_ban-sync-search-requesting-guild', {
        valueField: 'Id',
        searchField: 'Name',
        options: banSyncAvailableGuilds,
        items: selectedValue === '0' ? [] : [selectedValue],
        render: {
            option: function(data, escape) {
                return '<div>'
                    + '<span>'
                    + '<img src="' + escape(data.IconUrl == null ? 'DebugEmpty.png' : data.IconUrl.replaceAll('~/', '')) + '" class="card-img-top smallUserIcon" width="32" height="32" alt="" />'
                    + ' ' + escape(data.Name)
                    + '</span>'
                    + '</div>';
            },
            item: function(data, escape) {
                return '<div title="' + escape(data.Id) + '">'
                    + '<img src="' + escape(data.IconUrl == null ? 'DebugEmpty.png' : data.IconUrl.replaceAll('~/', '')) + '" class="card-img-top smallUserIcon" width="32" height="32" alt="" />'
                    + '<span class="ps-1">'
                    + escape(data.Name)
                    + '</span>'
                    + '</div>';
            }
        }
    });
}

$(document).on('htmx:afterSettle', (ev) => {
    console.log(ev)
    if (ev.target === document.getElementById('ban-search-root')) {
        hookSelect();
        // setTimeout(hookSelect, 200);
    }
})
$(document).ready(function() {
    hookSelect();
});