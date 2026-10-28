(function () {
    const plan = document.getElementById('floorplan');
    if (!plan) {
        return;
    }
    const toggle = document.getElementById('edit-toggle');
    const tools = document.getElementById('floorplan-tools');
    const cell = Number(plan.dataset.cell);
    const token = plan.querySelector('input[name="__RequestVerificationToken"]').value;
    const minSize = 2;

    const setEditing = editing => {
        plan.classList.toggle('editing', editing);
        tools.classList.toggle('editing', editing);
        toggle.textContent = editing ? 'Done' : 'Edit Layout';
    };
    toggle.addEventListener('click', () => setEditing(!plan.classList.contains('editing')));

    // Draw a room at its grid position and keep the plan tall enough to hold every room.
    const place = room => {
        room.style.left = `${room.dataset.x * cell}px`;
        room.style.top = `${room.dataset.y * cell}px`;
        room.style.width = `${room.dataset.width * cell}px`;
        room.style.height = `${room.dataset.height * cell}px`;
        const bottom = Math.max(...[...plan.querySelectorAll('.room')].map(item => Number(item.dataset.y) + Number(item.dataset.height)));
        plan.style.height = `${(bottom + 2) * cell}px`;
    };

    const save = room => fetch(plan.dataset.moveUrl, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json', RequestVerificationToken: token },
        body: JSON.stringify({ id: Number(room.dataset.id), x: Number(room.dataset.x), y: Number(room.dataset.y), width: Number(room.dataset.width), height: Number(room.dataset.height) })
    });

    // Dragging the header moves the room; dragging the corner handle resizes it. Both snap to the grid.
    const track = (event, room, update) => {
        const startX = event.clientX;
        const startY = event.clientY;
        const start = { x: Number(room.dataset.x), y: Number(room.dataset.y), width: Number(room.dataset.width), height: Number(room.dataset.height) };
        const target = event.currentTarget;
        target.setPointerCapture(event.pointerId);
        const move = moveEvent => {
            update(start, Math.round((moveEvent.clientX - startX) / cell), Math.round((moveEvent.clientY - startY) / cell));
            place(room);
        };
        const stop = () => {
            target.removeEventListener('pointermove', move);
            target.removeEventListener('pointerup', stop);
            save(room);
        };
        target.addEventListener('pointermove', move);
        target.addEventListener('pointerup', stop);
    };

    plan.querySelectorAll('.room').forEach(room => {
        room.querySelector('.room-header').addEventListener('pointerdown', event => {
            if (!plan.classList.contains('editing') || event.target.closest('form')) {
                return;
            }
            track(event, room, (start, dx, dy) => {
                room.dataset.x = Math.max(0, start.x + dx);
                room.dataset.y = Math.max(0, start.y + dy);
            });
        });
        room.querySelector('.room-resize').addEventListener('pointerdown', event => {
            track(event, room, (start, dx, dy) => {
                room.dataset.width = Math.max(minSize, start.width + dx);
                room.dataset.height = Math.max(minSize, start.height + dy);
            });
        });
    });
}());
