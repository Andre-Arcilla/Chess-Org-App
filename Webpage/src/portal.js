import { supabase } from './db.js';

const loginSection = document.getElementById('login-section');
const registerSection = document.getElementById('register-section');
const showRegisterLink = document.getElementById('show-register');
const showLoginLink = document.getElementById('show-login');
const formTitle = document.getElementById('form-title');
const formSubtitle = document.getElementById('form-subtitle');
const loginForm = document.getElementById('login-form');
const emailInput = document.getElementById('email');
const passwordInput = document.getElementById('password');
const loginBtn = document.getElementById('login-btn');
const registerForm = document.getElementById('register-form');
const regNameInput = document.getElementById('reg-name');
const regIdInput = document.getElementById('reg-id');
const regEmailInput = document.getElementById('reg-email');
const regPasswordInput = document.getElementById('reg-password');
const registerBtn = document.getElementById('register-btn');

// Clear custom validities dynamically as user types
emailInput.addEventListener('input', () => emailInput.setCustomValidity(''));
passwordInput.addEventListener('input', () => passwordInput.setCustomValidity(''));
regNameInput.addEventListener('input', () => regNameInput.setCustomValidity(''));
regIdInput.addEventListener('input', () => regIdInput.setCustomValidity(''));
regEmailInput.addEventListener('input', () => regEmailInput.setCustomValidity(''));
regPasswordInput.addEventListener('input', () => regPasswordInput.setCustomValidity(''));

/*
 * Toggle UI between Login and Register
 */
function toggleForms(e, showForm) {
    e.preventDefault();
    
    // Clear any lingering native error bubbles
    emailInput.setCustomValidity('');
    passwordInput.setCustomValidity('');
    regNameInput.setCustomValidity('');
    regIdInput.setCustomValidity('');
    regEmailInput.setCustomValidity('');
    regPasswordInput.setCustomValidity('');
    
    if (showForm === 'register') {
        loginSection.hidden = true;
        registerSection.hidden = false;
    } else {
        registerSection.hidden = true;
        loginSection.hidden = false;
    }
}

/**
 * Handle user login using native browser error tooltips
 */
async function handleLogin(e) {
    e.preventDefault();
    
    const email = emailInput.value.trim();
    const password = passwordInput.value;
    
    loginBtn.disabled = true;
    loginBtn.textContent = 'Verifying Membership...';
    
    try {
        const { data: profiles, error: profileError } = await supabase
            .schema('Chessistant')
            .from('Profiles')
            .select('*')
            .eq('Email', email)
            .limit(1);

        if (profileError) throw profileError;
        
        // 1. User not found flag on Email field
        if (!profiles || profiles.length === 0) {
            emailInput.setCustomValidity('No account found matching this email address.');
            loginForm.reportValidity();
            return;
        }

        const profile = profiles[0];  
        
        // 2. Incorrect password flag on Password field
        if (profile.Password != password) {
            passwordInput.setCustomValidity('Incorrect password. Please try again.');
            loginForm.reportValidity();
            return;
        }

        const safeUserData = {
            StudName: profile.StudName,
            StudNum: profile.StudNum,
            Role: profile.Role,
            Email: profile.Email
        };
        localStorage.setItem('currentUser', JSON.stringify(safeUserData));

        if (profile.Role === 'Admin') {
            window.location.href = '/dashboard-ADMIN.html';
        } else if (profile.Role === 'Coach') {
            window.location.href = '/dashboard-COACH.html';
        } else if (profile.Role === 'Member') {
            window.location.href = '/dashboard-MEMBER.html';
        }  else if (profile.Role === 'Disabled') {
            emailInput.setCustomValidity('This account has been deactivated.');
            loginForm.reportValidity();
        }  else {
            emailInput.setCustomValidity('This account holds an unassigned role profile.');
            loginForm.reportValidity();
        }
    } catch (error) {
        console.error('Login Error:', error.message);
        emailInput.setCustomValidity('System connection failed: ' + error.message);
        loginForm.reportValidity();
    } finally {
        loginBtn.disabled = false;
        loginBtn.textContent = 'Enter Club';
    }
}

/**
 * Handle user registration using native browser error tooltips
 */
async function handleRegister(e) {
    e.preventDefault();

    const name = regNameInput.value.trim();
    const studId = regIdInput.value.trim();
    const email = regEmailInput.value.trim();
    const password = regPasswordInput.value;

    registerBtn.disabled = true;
    registerBtn.textContent = 'Sending Application...';

    // FRONT-END VALIDATIONS
    try {
        // 1. Prevent empty or whitespace-only strings
        if (!name || !studId || !email || !password) {
            if (!name) regNameInput.setCustomValidity('Full Name cannot be left blank.');
            else if (!studId) regIdInput.setCustomValidity('Student ID cannot be left blank.');
            else if (!email) regEmailInput.setCustomValidity('Email cannot be left blank.');
            else if (!password) regPasswordInput.setCustomValidity('Password cannot be left blank.');
            
            registerForm.reportValidity();
            return; // Triggers finally block to re-enable button
        }

        // 2. Validate Student ID format (1 letter followed by exactly 8 digits)
        const studIdRegex = /^[A-Za-z]\d{8}$/;
        if (!studIdRegex.test(studId)) {
            regIdInput.setCustomValidity('Student ID format must be 1 letter followed by 8 numbers (e.g., A12345678).');
            registerForm.reportValidity();
            return;
        }

        // 3. Restrict email domain to 'umak.edu.ph'
        if (!email.toLowerCase().endsWith('@umak.edu.ph')) {
            regEmailInput.setCustomValidity('Registration is restricted to official UMak emails (@umak.edu.ph) only.');
            registerForm.reportValidity();
            return;
        }

        // DATABASE DUPLICATION CHECKS
        // Fetch database tables to verify duplicate parameters
        const [profileCheck, registrationCheck] = await Promise.all([
            supabase
                .schema('Chessistant')
                .from('Profiles')
                .select('Email, StudNum')
                .or(`Email.eq.${email},StudNum.eq.${studId}`),
            supabase
                .schema('Chessistant')
                .from('Registrations')
                .select('Email, StudNum')
                .or(`Email.eq.${email},StudNum.eq.${studId}`)
        ]);

        if (profileCheck.error) throw profileCheck.error;
        if (registrationCheck.error) throw registrationCheck.error;

        // Check Active Profiles Row Matches
        if (profileCheck.data && profileCheck.data.length > 0) {
            const match = profileCheck.data[0];
            if (match.Email.toLowerCase() === email.toLowerCase()) {
                regEmailInput.setCustomValidity('An account already uses this email.');
                registerForm.reportValidity();
            } else {
                regIdInput.setCustomValidity('An account already uses this Student ID.');
                registerForm.reportValidity();
            }
            return; 
        }

        // Check Pending Applications Row Matches
        if (registrationCheck.data && registrationCheck.data.length > 0) {
            const match = registrationCheck.data[0];
            if (match.Email.toLowerCase() === email.toLowerCase()) {
                regEmailInput.setCustomValidity('A pending application with this email is already awaiting admin review.');
                registerForm.reportValidity();
            } else {
                regIdInput.setCustomValidity('A pending application with this Student ID is already awaiting admin review.');
                registerForm.reportValidity();
            }
            return;
        }

        // Submit safely to database
        const payload = {
            StudName: name,
            StudNum: studId,
            Email: email,
            Password: password,
            Date: new Date().toISOString()
        };

        const { error } = await supabase
            .schema('Chessistant')
            .from('Registrations')
            .insert([payload]);

        if (error) throw error;

        // Clean success actions
        registerForm.reset();
        window.alert('Application submitted successfully! Please wait for an Admin to review your registration.');
        toggleForms(new Event('click'), 'login');

    } catch (error) {
        console.error('Registration Error:', error.message);
        regEmailInput.setCustomValidity('Registration failed: ' + error.message);
        registerForm.reportValidity();
    } finally {
        registerBtn.disabled = false;
        registerBtn.textContent = 'Submit Registration';
    }
}

// Event Listeners
if (showRegisterLink) showRegisterLink.addEventListener('click', (e) => toggleForms(e, 'register'));
if (showLoginLink) showLoginLink.addEventListener('click', (e) => toggleForms(e, 'login'));
if (loginForm) loginForm.addEventListener('submit', handleLogin);
if (registerForm) registerForm.addEventListener('submit', handleRegister);