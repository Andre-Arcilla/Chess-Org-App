import React, { useEffect, useState } from 'react';
import { createRoot } from 'react-dom/client';
import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import MemberDashboardLayout from './components/DashboardLayout-MEMBER';
import MemberHome from './pages-MEMBER/MemberHome';
import MemberProfile from './pages-MEMBER/MemberProfile';
import MemberTournaments from './pages-MEMBER/MemberTournaments';

const MemberBouncer = ({ children }) => {
    const [loading, setLoading] = useState(true);
    const [authorized, setAuthorized] = useState(false);
    const [memberData, setMemberData] = useState(null);

    useEffect(() => {
        const checkSession = async () => {
        try {
            const storedUser = localStorage.getItem('currentUser');
            
            if (!storedUser) {
            window.location.href = '/index.html';
            return;
            }

            const profile = JSON.parse(storedUser);

            // Verify user has Member role
            if (profile.Role !== 'Member') {
            console.error('Unauthorized access');
            localStorage.removeItem('currentUser');
            window.location.href = '/index.html';
            return;
            }

            setMemberData(profile);
            setAuthorized(true);
        } catch (err) {
            console.error('MemberBouncer error:', err);
            window.location.href = '/index.html';
        } finally {
            setLoading(false);
        }
        };

        checkSession();
    }, []);

    if (loading) {
        return (
        <div className="overlay">
            <div className="spinner"></div>
            <p>Loading Member Portal...</p>
        </div>
        );
    }

    if (!authorized) return null;

    return React.cloneElement(children, { memberData });
};

const App = () => {
    return (
        <BrowserRouter>
            <Routes>
                <Route 
                path="/" 
                element={
                    <MemberBouncer>
                    <MemberDashboardLayout />
                    </MemberBouncer>
                }
                >
                <Route index element={<MemberHome />} />
                <Route path="profile" element={<MemberProfile />} />
                <Route path="tournaments" element={<MemberTournaments />} />
                <Route path="*" element={<Navigate to="/" replace />} />
                </Route>
            </Routes>
        </BrowserRouter>
    );
};

const container = document.getElementById('root');
const root = createRoot(container);
root.render(<App />);
