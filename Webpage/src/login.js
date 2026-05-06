import { supabase } from './db.js';

const loginForm = document.getElementById('login-form');
const emailInput = document.getElementById('email');
const passwordInput = document.getElementById('password');
const errorMessage = document.getElementById('error-message');
const loginBtn = document.getElementById('login-btn');

/**
 * Handle user login
 */
async function handleLogin(e) {
    e.preventDefault();
    
    const email = emailInput.value.trim();
    const password = passwordInput.value;
    
    // UI Loading State
    loginBtn.disabled = true;
    loginBtn.textContent = 'Verifying Membership...';
    errorMessage.hidden = true;
    
    try {
        const { data, error } = await supabase.auth.signInWithPassword({
            email,
            password,
        });
        
        if (error) throw error;
        
        // Successful login: Redirect to Dashboard
        window.location.href = '/dashboard.html';
        
    } catch (error) {
        console.error('Login Error:', error.message);
        errorMessage.textContent = error.message;
        errorMessage.hidden = false;
        
        // Reset button
        loginBtn.disabled = false;
        loginBtn.textContent = 'Enter Club';
    }
}

// Event Listeners
if (loginForm) {
    loginForm.addEventListener('submit', handleLogin);
}
