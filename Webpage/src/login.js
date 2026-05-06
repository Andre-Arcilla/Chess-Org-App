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
        // const { data, error } = await supabase.auth.signInWithPassword({
        //     email,
        //     password,
        // });
        
        // if (error) throw error;

        // Successful login: Redirect to Dashboard
        // if (profile.Role === 'Admin') {
        //     window.location.href = '/dashboard.html';
        // }
        
        const { data: profiles, error: profileError } = await supabase
            .schema('Chessistant')
            .from('Profiles')
            .select('*')
            .eq('Email', email)
            .limit(1);

        console.log('Profile query result:', { profiles, profileError });

        // Verify errors
        if (profileError) throw new Error(profileError.message || 'User not found');
        if (!profiles || profiles.length === 0) throw new Error('User not found');

        const profile = profiles[0];  // Use first row if multiples exist
        if (profile.Password != password) throw new Error('Invalid password');

        // Store only safe user data in localStorage (no password)
        const safeUserData = {
            StudName: profile.StudName,
            StudNum: profile.StudNum,
            Role: profile.Role,
            Email: profile.Email
        };
        localStorage.setItem('currentUser', JSON.stringify(safeUserData));

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
