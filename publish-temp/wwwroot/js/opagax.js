/**
 * Opagax Custom JavaScript
 * Helpers et utilitaires pour l'interface Opagax
 * Adapté de Formatix pour ASP.NET Core
 */

// Configuration
const OpagaxConfig = {
    apiBaseUrl: window.location.origin,
    toastContainer: null
};

// Initialisation
document.addEventListener('DOMContentLoaded', function() {
    OpagaxConfig.toastContainer = document.getElementById('toastContainer');
    if (!OpagaxConfig.toastContainer) {
        // Créer le container s'il n'existe pas
        const container = document.createElement('div');
        container.id = 'toastContainer';
        container.className = 'toast-container position-fixed bottom-0 end-0 p-3';
        document.body.appendChild(container);
        OpagaxConfig.toastContainer = container;
    }
});

/**
 * Appels API avec gestion automatique des erreurs
 * Compatible avec ASP.NET Core (pas de JWT par défaut, mais prêt si besoin)
 */
async function apiCall(path, options = {}) {
    const url = path.startsWith('http') ? path : `${OpagaxConfig.apiBaseUrl}${path}`;
    const headers = Object.assign({ 'Content-Type': 'application/json' }, options.headers || {});
    
    // Si un token est stocké (pour futures améliorations)
    const token = localStorage.getItem('opagax_access_token');
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }
    
    try {
        const resp = await fetch(url, { ...options, headers });
        let data = null;
        try {
            data = await resp.json();
        } catch (_) {
            // Pas de JSON dans la réponse
        }
        return { status: resp.status, data, ok: resp.ok };
    } catch (error) {
        return { status: 0, data: null, ok: false, error: error.message };
    }
}

/**
 * Système de notifications Toast (Bootstrap 5)
 */
const Toast = {
    show: function(message, type = 'info', duration = 5000) {
        if (!OpagaxConfig.toastContainer) {
            console.error('Toast container not found');
            return;
        }
        
        const toastId = 'toast-' + Date.now();
        const bgClass = {
            'success': 'bg-success',
            'error': 'bg-danger',
            'warning': 'bg-warning',
            'info': 'bg-info'
        }[type] || 'bg-info';
        
        const icon = {
            'success': 'check-circle-fill',
            'error': 'exclamation-triangle-fill',
            'warning': 'exclamation-triangle-fill',
            'info': 'info-circle-fill'
        }[type] || 'info-circle-fill';
        
        const toastHtml = `
            <div id="${toastId}" class="toast" role="alert" aria-live="assertive" aria-atomic="true" data-bs-delay="${duration}">
                <div class="toast-header ${bgClass} text-white">
                    <i class="fas fa-${icon === 'check-circle-fill' ? 'check-circle' : icon === 'exclamation-triangle-fill' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
                    <strong class="me-auto">Opagax</strong>
                    <button type="button" class="btn-close btn-close-white" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
                <div class="toast-body">
                    ${message}
                </div>
            </div>
        `;
        
        OpagaxConfig.toastContainer.insertAdjacentHTML('beforeend', toastHtml);
        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement);
        toast.show();
        
        // Nettoyer après fermeture
        toastElement.addEventListener('hidden.bs.toast', function() {
            toastElement.remove();
        });
    },
    
    success: function(message) {
        this.show(message, 'success');
    },
    
    error: function(message) {
        this.show(message, 'error');
    },
    
    warning: function(message) {
        this.show(message, 'warning');
    },
    
    info: function(message) {
        this.show(message, 'info');
    }
};

/**
 * Gestion des états de chargement
 */
const Loading = {
    show: function(element) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        if (element) {
            element.disabled = true;
            const originalHTML = element.innerHTML;
            element.dataset.originalHtml = originalHTML;
            element.innerHTML = '<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>Chargement...';
        }
    },
    
    hide: function(element) {
        if (typeof element === 'string') {
            element = document.querySelector(element);
        }
        if (element && element.dataset.originalHtml) {
            element.innerHTML = element.dataset.originalHtml;
            element.disabled = false;
            delete element.dataset.originalHtml;
        }
    }
};

/**
 * Formatage JSON pour affichage
 */
function formatJSON(obj) {
    try {
        return JSON.stringify(obj, null, 2);
    } catch (e) {
        return String(obj);
    }
}

/**
 * Mise à jour de l'affichage de statut
 */
function updateStatus(element, message, isSuccess) {
    if (typeof element === 'string') {
        element = document.querySelector(element);
    }
    if (element) {
        element.textContent = message;
        element.className = 'status ' + (isSuccess ? 'text-success' : 'text-danger');
    }
}

/**
 * Confirmation de suppression (helper)
 */
function confirmDelete(message = 'Êtes-vous sûr de vouloir supprimer cet élément ?') {
    return confirm(message);
}

/**
 * Export pour utilisation globale
 */
window.Opagax = {
    apiCall,
    Toast,
    Loading,
    formatJSON,
    updateStatus,
    confirmDelete,
    Config: OpagaxConfig
};
