(function () {
    const data = document.getElementById('schedule-data');
    const details = document.getElementById('schedule-details');
    if (!data || !details) {
        return;
    }
    const tasks = new Map(JSON.parse(data.textContent).map(task => [task.id, task]));

    const element = (tag, className, text) => {
        const item = document.createElement(tag);
        if (className) {
            item.className = className;
        }
        if (text !== undefined && text !== null) {
            item.textContent = text;
        }
        return item;
    };
    const section = title => {
        const part = element('section', 'details-section');
        if (title) {
            part.append(element('h3', null, title));
        }
        details.append(part);
        return part;
    };
    const rows = (parent, pairs) => {
        const list = element('dl', 'details-rows');
        pairs.filter(([, value]) => value !== null && value !== undefined && value !== '').forEach(([label, value]) => {
            list.append(element('dt', null, label));
            const cell = element('dd');
            cell.append(value);
            list.append(cell);
        });
        parent.append(list);
    };

    // Shows a task's details and marks every one of its runs, so its other blocks stand out across the bars.
    const show = id => {
        const task = tasks.get(id);
        if (!task) {
            return;
        }
        document.querySelectorAll('[data-task]').forEach(item => item.classList.toggle('selected', Number(item.dataset.task) === id));
        details.replaceChildren();
        const head = element('div', 'details-head');
        head.append(element('strong', null, task.name));
        const badges = element('div', 'details-badges');
        badges.append(element('span', 'details-badge', task.source));
        badges.append(element('span', `details-badge ${task.active ? 'online' : 'offline'}`, task.active ? 'On' : 'Off'));
        if (task.lastStatus) {
            badges.append(element('span', `details-badge ${task.lastStatus === 'success' ? 'online' : 'failed'}`, task.lastStatus === 'success' ? 'Last Run OK' : `Last Run ${task.lastStatus}`));
        }
        head.append(badges);
        details.append(head);
        // The host opens the map on that machine when it's one HoLaMap knows.
        let host = task.host;
        if (task.device) {
            host = element('a', 'details-link', task.host);
            host.href = `${details.dataset.mapUrl}?device=${encodeURIComponent(task.device)}`;
        }
        rows(section(), [['Host', host], ['Inventory', task.target !== task.host ? task.target : ''], ['Runs', task.command], ['Schedule', task.schedule], ['Cron', task.cron]]);
        rows(section('Runs'), [['Last Run', task.lastRun || 'Not yet'], ['Usually Takes', task.typical || 'Not known yet'], ['Recent', task.durations.join(', ')]]);
    };

    document.addEventListener('click', event => {
        const target = event.target.closest('[data-task]');
        if (target) {
            show(Number(target.dataset.task));
        }
    });
    document.addEventListener('keydown', event => {
        if (event.key === 'Escape') {
            document.querySelectorAll('[data-task].selected').forEach(item => item.classList.remove('selected'));
            details.textContent = 'Select a task to see its details.';
        }
    });
})();