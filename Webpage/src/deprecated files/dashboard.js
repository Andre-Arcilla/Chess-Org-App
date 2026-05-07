import { supabase } from '../db.js';

const loadingOverlay = document.getElementById('loading-overlay');
const dashboardContent = document.getElementById('dashboard-content');
const adminNameSpan = document.getElementById('admin-name');
const logoutBtn = document.getElementById('logout-btn');

/**
 * The Bouncer: Security and Role Verification
 */
async function initDashboard() {
    try {
        // 1. Session Check
        const { data: { session }, error: sessionError } = await supabase.auth.getSession();
        
        if (sessionError || !session) {
            console.log('No active session found. Redirecting to login.');
            window.location.href = '/index.html';
            return;
        }

        const userEmail = session.user.email;

        // 2. Role Verification
        // Query the Profiles table in the 'Chessistant' schema (configured in db.js)
        const { data: profile, error: profileError } = await supabase
            .from('Profiles')
            .select('Email, Role, StudName')
            .eq('Email', userEmail)
            .single();

        if (profileError || !profile) {
            console.error('Authorization Error: Profile not found.', profileError);
            alert('Access Denied: You do not have an active membership profile.');
            await supabase.auth.signOut();
            window.location.href = '/index.html';
            return;
        }

        // Strict Role Check: Must perfectly match "Admin"
        if (profile.Role !== 'Admin') {
            console.warn(`Access Denied: User role is "${profile.Role}", not "Admin".`);
            alert('Access Denied: This area is reserved for Grandmaster Admins.');
            await supabase.auth.signOut();
            window.location.href = '/index.html';
            return;
        }

        // 3. Success: Grant Entry
        adminNameSpan.textContent = profile.StudName || 'Admin';
        
        // Remove loading state
        loadingOverlay.style.display = 'none';
        dashboardContent.hidden = false;

    } catch (err) {
        console.error('Critical Dashboard Failure:', err);
        window.location.href = '/index.html';
    }
}

/**
 * Handle Logout
 */
if (logoutBtn) {
    logoutBtn.addEventListener('click', async () => {
        const { error } = await supabase.auth.signOut();
        if (error) console.error('Logout error:', error.message);
        window.location.href = '/index.html';
    });
}

// Execute Bouncer
initDashboard();
