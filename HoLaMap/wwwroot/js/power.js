(function () {
    const table = document.querySelector('.power-table');
    const message = document.getElementById('power-message');
    if (!table) {
        return;
    }

    // Shows whether each machine is up, and only enables the buttons that make sense for it.
    const refresh = async () => {
        try {
            const response = await fetch(table.dataset.statusUrl);
            const statuses = await response.json();
            statuses.forEach(status => {
                const row = table.querySelector(`tr[data-id="${status.id}"]`);
                if (!row) {
                    return;
                }
                const badge = row.querySelector('.power-status');
                badge.className = `power-status ${status.online ? 'online' : 'offline'}`;
                badge.textContent = status.online ? 'Online' : 'Offline';
                row.querySelectorAll('.power-wake').forEach(button => button.disabled = status.online);
                row.querySelectorAll('.power-down').forEach(button => button.disabled = !status.online);
            });
        } catch (error) {
            console.error(error);
        }
    };

    table.addEventListener('click', event => {
        const button = event.target.closest('[data-confirm]');
        if (button && !confirm(button.dataset.confirm)) {
            event.preventDefault();
        }
    });

    // Sends the action without leaving the page, then shows what happened.
    table.addEventListener('submit', async event => {
        event.preventDefault();
        const button = event.submitter;
        button.disabled = true;
        try {
            const response = await fetch(event.target.action, { method: 'POST', body: new FormData(event.target), headers: { 'X-Requested-With': 'fetch' } });
            message.textContent = (await response.json()).message;
        } catch (error) {
            console.error(error);
            message.textContent = 'The request failed.';
        }
        message.hidden = false;
        refresh();
    });

    refresh();
    setInterval(refresh, Number(table.dataset.refresh));
})();
