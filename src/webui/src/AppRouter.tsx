import React from 'react';
import { BrowserRouter, Routes, Route, Link } from 'react-router-dom';
import App from './components/App';
import AdminPage from './components/AdminPage';
import './styles/Navigation.css';
import logo from './logo.svg';

const Navigation = () => (
    <nav className="main-nav">
        <div className="nav-container">
            <div className="nav-left">
                <div className="logo">
                    <img src={logo} alt="Accede Logo" />
                    <h1>Accede Concierge</h1>
                </div>
                <ul className="nav-links">
                    <li><Link to="/">My Trips</Link></li>
                    <li><Link to="/admin">Admin</Link></li>
                </ul>
            </div>
            <div className="user-info">
                <span className="user-name">Terry</span>
                <div className="user-avatar">
                    <svg viewBox="0 0 24 24" width="24" height="24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M20 21V19C20 17.9391 19.5786 16.9217 18.8284 16.1716C18.0783 15.4214 17.0609 15 16 15H8C6.93913 15 5.92172 15.4214 5.17157 16.1716C4.42143 16.9217 4 17.9391 4 19V21" stroke="#4A5568" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        <path d="M12 11C14.2091 11 16 9.20914 16 7C16 4.79086 14.2091 3 12 3C9.79086 3 8 4.79086 8 7C8 9.20914 9.79086 11 12 11Z" stroke="#4A5568" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    </svg>
                </div>
            </div>
        </div>
    </nav>
);

const AppRouter: React.FC = () => (
    <BrowserRouter>
        <Navigation />
        <Routes>
            <Route path="/" element={<App />} />
            <Route path="/admin" element={<AdminPage />} />
        </Routes>
    </BrowserRouter>
);

export default AppRouter;
