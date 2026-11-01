const App = {
    templates: [],
    isInitialized: false,

    async init() {
        if (this.isInitialized) return;
        this.isInitialized = true;

        const tomorrow = new Date(Date.now() + 86400000);
        document.getElementById('startTime').value = tomorrow.toISOString().slice(0, 16);
        
        this.bindEvents();
        await this.loadTemplates();
        await this.loadEvents();
    },

    bindEvents() {
        const createForm = document.getElementById('createEventForm');
        const registerForm = document.getElementById('registerForm');

        createForm.replaceWith(createForm.cloneNode(true));
        registerForm.replaceWith(registerForm.cloneNode(true));

        document.getElementById('createEventForm').addEventListener('submit', (e) => this.handleCreateEvent(e));
        document.getElementById('registerForm').addEventListener('submit', (e) => this.handleRegisterPlayer(e));
        
        document.getElementById('gameId').addEventListener('change', (e) => {
            this.updateFormatDropdown(e.target.value);
        });
    },

    async loadTemplates() {
        try {
            const res = await fetch('/api/Events/templates');
            if (!res.ok) throw new Error('Failed to load templates.');
            
            this.templates = await res.json();
            
            const gameSelect = document.getElementById('gameId');
            gameSelect.innerHTML = this.templates.map(t => `
                <option value="${t.gameId}">${t.name}</option>
            `).join('');

            if (this.templates.length > 0) {
                this.updateFormatDropdown(this.templates[0].gameId);
            }
        } catch (err) {
            console.error("Template loading error:", err);
        }
    },

    updateFormatDropdown(gameId) {
        const formatSelect = document.getElementById('gameFormatId');
        const selectedTemplate = this.templates.find(t => t.gameId === gameId);

        if (!selectedTemplate || !selectedTemplate.supportedFormats || !selectedTemplate.supportedFormats.length) {
            formatSelect.innerHTML = '<option value="">No formats available</option>';
            return;
        }

        formatSelect.innerHTML = selectedTemplate.supportedFormats.map(f => `
            <option value="${f.id}">${f.name} (${f.defaultDurationMinutes} mins)</option>
        `).join('');
    },

    async loadEvents() {
        try {
            const res = await fetch('/api/Events');
            if (!res.ok) throw new Error('Failed to fetch events.');
            
            const events = await res.json();
            const container = document.getElementById('eventsList');
            
            if (!events.length) {
                container.innerHTML = '<p class="text-placeholder">No events scheduled yet. Create one on the left.</p>';
                return;
            }

            container.innerHTML = events.map(e => `
                <div class="event-card">
                    <div>
                        <div style="display: flex; align-items: center; gap: 0.5rem; margin-bottom: 0.375rem;">
                            <h3 class="event-title">${e.name}</h3>
                            <span class="badge badge-info">${e.gameTemplate?.name || e.gameId}</span>
                        </div>
                        <p class="event-meta">
                            🕒 ${new Date(e.startTime).toLocaleString()} &bull; 📍 ${e.location}
                        </p>
                    </div>
                    <div style="display: flex; flex-direction: column; align-items: flex-end; gap: 0.5rem;">
                        <span class="badge ${e.registrations.length >= e.capacity ? 'badge-danger' : 'badge-success'}">
                            Seats: ${e.registrations.length} / ${e.capacity}
                        </span>
                        <button class="btn btn-outline-light" onclick='App.openModal(${JSON.stringify(e)})'>
                            ⚙️ Manage & QR
                        </button>
                    </div>
                </div>
            `).join('');
        } catch (err) {
            console.error("Failed to load events:", err);
            document.getElementById('eventsList').innerHTML = '<p class="text-placeholder" style="color: #dc2626;">Error loading events from server.</p>';
        }
    },

    async handleCreateEvent(e) {
        e.preventDefault();
        
        const submitBtn = e.target.querySelector('button[type="submit"]');
        if (submitBtn) submitBtn.disabled = true;

        const payload = {
            name: document.getElementById('eventName').value,
            gameId: document.getElementById('gameId').value,
            gameFormatId: parseInt(document.getElementById('gameFormatId').value),
            startTime: new Date(document.getElementById('startTime').value).toISOString(),
            capacity: parseInt(document.getElementById('capacity').value),
            location: document.getElementById('location').value
        };

        try {
            const res = await fetch('/api/Events', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            if (res.ok) {
                await this.loadEvents();
            } else {
                let errorMsg = 'Failed to create event.';
                const errData = await res.json().catch(() => null);
                if (errData) {
                    if (errData.errors) {
                        errorMsg = Object.values(errData.errors).flat().join(' ');
                    } else {
                        errorMsg = errData.message || errData.title || JSON.stringify(errData);
                    }
                }
                alert(`Error: ${errorMsg}`);
            }
        } catch (err) {
            console.error('Network error during event creation:', err);
            alert('An unexpected network error occurred.');
        } finally {
            if (submitBtn) submitBtn.disabled = false;
        }
    },

    openModal(event) {
        document.getElementById('modalEventTitle').innerText = event.name;
        document.getElementById('modalEventId').value = event.id;
        document.getElementById('modalEventMeta').innerHTML = `<strong>Location:</strong> ${event.location}<br><strong>Time:</strong> ${new Date(event.startTime).toLocaleString()}`;
        document.getElementById('modalQrCode').src = `/api/Events/${event.id}/qrcode`;
        document.getElementById('modalIcsBtn').href = `/api/Events/${event.id}/ics`;
        document.getElementById('regCount').innerText = event.registrations.length;
        document.getElementById('regCapacity').innerText = event.capacity;
        document.getElementById('registrationAlert').innerHTML = '';

        const playerList = document.getElementById('registeredPlayersList');
        playerList.innerHTML = event.registrations.length === 0 
            ? '<li class="roster-item roster-empty">No registrations recorded.</li>'
            : event.registrations.map(r => `<li class="roster-item"><strong>${r.playerName}</strong> &lt;${r.playerEmail}&gt;</li>`).join('');

        document.getElementById('eventModal').style.display = 'flex';
    },

    closeModal() {
        document.getElementById('eventModal').style.display = 'none';
    },

    async handleRegisterPlayer(e) {
        e.preventDefault();
        const eventId = document.getElementById('modalEventId').value;
        const payload = {
            playerName: document.getElementById('playerName').value,
            playerEmail: document.getElementById('playerEmail').value
        };

        const alertBox = document.getElementById('registrationAlert');

        try {
            const res = await fetch(`/api/Events/${eventId}/register`, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(payload)
            });

            const data = await res.json().catch(() => ({}));

            if (res.ok) {
                alertBox.innerHTML = `<div style="padding: 0.5rem; background: rgba(34, 197, 94, 0.1); color: #16a34a; border-radius: 6px; font-size: 0.85rem;">${data.message || 'Registration successful!'}</div>`;
                document.getElementById('registerForm').reset();
                await this.loadEvents();
                
                const countElem = document.getElementById('regCount');
                countElem.innerText = parseInt(countElem.innerText || '0') + 1;
                
                const rosterList = document.getElementById('registeredPlayersList');
                if (rosterList.querySelector('.roster-empty')) {
                    rosterList.innerHTML = '';
                }
                rosterList.innerHTML += `<li class="roster-item"><strong>${payload.playerName}</strong> &lt;${payload.playerEmail}&gt;</li>`;
            } else {
                alertBox.innerHTML = `<div style="padding: 0.5rem; background: rgba(239, 68, 68, 0.1); color: #dc2626; border-radius: 6px; font-size: 0.85rem;">${data.message || 'Registration failed or capacity exceeded.'}</div>`;
            }
        } catch (err) {
            console.error('Registration network error:', err);
            alertBox.innerHTML = `<div style="padding: 0.5rem; background: rgba(239, 68, 68, 0.1); color: #dc2626; border-radius: 6px; font-size: 0.85rem;">Network error connecting to server.</div>`;
        }
    }
};

document.addEventListener('DOMContentLoaded', () => App.init());