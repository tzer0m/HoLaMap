(function () {
    const table = document.querySelector('.power-table');
    const message = document.getElementById('power-message');
    if (!table) {
        return;
    }

    // Machines waiting to change state after an action, by ID. Their buttons stay off until the change has held for a while, or until it's given up on.
    const pending = new Map();
    const settleMs = 15000;
    const giveUpMs = 180000;

    // Shows whether each machine is up, and only enables the buttons that make sense for it. Buttons start off until the first check is back.
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
                const wanted = pending.get(status.id);
                if (wanted) {
                    const now = Date.now();
                    if (wanted.online === status.online) {
                        wanted.settledAt ??= now + settleMs;
                    }
                    if (now >= wanted.giveUpAt || (wanted.settledAt && now >= wanted.settledAt)) {
                        pending.delete(status.id);
                    }
                }
                const waiting = pending.has(status.id);
                row.querySelectorAll('.power-wake').forEach(button => button.disabled = waiting || status.online);
                row.querySelectorAll('.power-down').forEach(button => button.disabled = waiting || !status.online);
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
        const row = button.closest('tr');
        row.querySelectorAll('button').forEach(item => item.disabled = true);
        try {
            const response = await fetch(event.target.action, { method: 'POST', body: new FormData(event.target), headers: { 'X-Requested-With': 'fetch' } });
            const result = await response.json();
            message.textContent = result.message;
            if (result.success) {
                pending.set(Number(row.dataset.id), { online: button.classList.contains('power-wake'), giveUpAt: Date.now() + giveUpMs });
            }
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
