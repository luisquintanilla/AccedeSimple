import React, { useEffect, useState } from 'react';
import AdminService from '../services/AdminService';
import { TripRequest, TripRequestStatus } from '../types/AdminTypes';
import '../styles/AdminPage.css';

const AdminPage: React.FC = () => {
    const [requests, setRequests] = useState<TripRequest[]>([]);
    const [loading, setLoading] = useState(true);
    const [error, setError] = useState<string | null>(null);
    const [selectedRequest, setSelectedRequest] = useState<TripRequest | null>(null);
    const [notes, setNotes] = useState('');
    const [submitting, setSubmitting] = useState(false);

    const adminService = AdminService.getInstance('/api');

    // Load requests
    const loadRequests = async () => {
        setLoading(true);
        setError(null);
        try {
            const data = await adminService.getRequests();
            setRequests(data);
        } catch (err) {
            setError('Failed to load requests. Please try again.');
            console.error(err);
        } finally {
            setLoading(false);
        }
    };

    useEffect(() => {
        loadRequests();
    }, []);

    // Submit a result for a request
    const submitResult = async (requestId: string, status: TripRequestStatus) => {
        setSubmitting(true);
        try {
            await adminService.submitResult(requestId, status, notes);
            // Refresh the list after submission
            await loadRequests();
            setSelectedRequest(null);
            setNotes('');
        } catch (err) {
            setError('Failed to submit result. Please try again.');
            console.error(err);
        } finally {
            setSubmitting(false);
        }
    };

    // Format date for display
    const formatDate = (dateStr: string) => {
        try {
            return new Date(dateStr).toLocaleString();
        } catch (e) {
            return dateStr;
        }
    };

    // Format currency for display
    const formatCurrency = (amount: number) => {
        return new Intl.NumberFormat('en-US', {
            style: 'currency',
            currency: 'USD'
        }).format(amount);
    };

    return (
        <div className="admin-container">
            <header className="admin-header">
                <div className="logo">
                    <h1>Accede Admin Portal</h1>
                </div>
                <div className="header-actions">
                    <button 
                        className="refresh-button"
                        onClick={loadRequests}
                        disabled={loading}
                    >
                        Refresh
                    </button>
                </div>
            </header>
            
            <div className="admin-content">
                {error && <div className="error-message">{error}</div>}
                
                {loading ? (
                    <div className="loading">Loading requests...</div>
                ) : (
                    <>
                        <h2>Pending Trip Requests</h2>
                        
                        {requests.length === 0 ? (
                            <div className="no-requests">No pending requests found</div>
                        ) : (
                            <div className="requests-container">
                                <div className="requests-list">
                                    {requests.map((request) => (
                                        <div 
                                            key={request.requestId} 
                                            className={`request-item ${selectedRequest?.requestId === request.requestId ? 'selected' : ''}`}
                                            onClick={() => setSelectedRequest(request)}
                                        >
                                            <div className="request-id">ID: {request.requestId}</div>
                                            <div className="request-destination">
                                                {request.tripOption.flights.length > 0 && (
                                                    <span>{request.tripOption.flights[0].origin} → {request.tripOption.flights[request.tripOption.flights.length-1].destination}</span>
                                                )}
                                            </div>
                                            <div className="request-cost">
                                                {formatCurrency(request.tripOption.totalCost)}
                                            </div>
                                        </div>
                                    ))}
                                </div>

                                {selectedRequest && (
                                    <div className="request-detail">
                                        <h3>Request Details</h3>
                                        <div className="detail-section">
                                            <h4>Trip Information</h4>
                                            <p className="detail-description">{selectedRequest.tripOption.description}</p>
                                            
                                            <h5>Flights</h5>
                                            <div className="flights-list">
                                                {selectedRequest.tripOption.flights.map((flight, index) => (
                                                    <div key={index} className="flight-item">
                                                        <div className="flight-header">
                                                            <span className="flight-number">{flight.airline} {flight.flightNumber}</span>
                                                            <span className="flight-date">{formatDate(flight.departureDateTime)}</span>
                                                        </div>
                                                        <div className="flight-route">
                                                            <span className="flight-origin">{flight.origin}</span>
                                                            <span className="flight-arrow">→</span>
                                                            <span className="flight-destination">{flight.destination}</span>
                                                        </div>
                                                        <div className="flight-details">
                                                            <span className="flight-time">Departure: {new Date(flight.departureDateTime).toLocaleTimeString()}</span>
                                                            <span className="flight-duration">{flight.duration}</span>
                                                            <span className="flight-price">{formatCurrency(flight.price)}</span>
                                                        </div>
                                                    </div>
                                                ))}
                                            </div>
                                            
                                            {selectedRequest.tripOption.hotel && (
                                                <>
                                                    <h5>Hotel</h5>
                                                    <div className="hotel-details">
                                                        <div className="hotel-name">{selectedRequest.tripOption.hotel.hotelName}</div>
                                                        <div className="hotel-address">{selectedRequest.tripOption.hotel.address}</div>
                                                        <div className="hotel-dates">
                                                            Check-in: {formatDate(selectedRequest.tripOption.hotel.checkIn)} | 
                                                            Check-out: {formatDate(selectedRequest.tripOption.hotel.checkOut)}
                                                        </div>
                                                        <div className="hotel-room">{selectedRequest.tripOption.hotel.roomType}</div>
                                                        <div className="hotel-price">
                                                            {formatCurrency(selectedRequest.tripOption.hotel.pricePerNight)} per night × 
                                                            {selectedRequest.tripOption.hotel.numberOfNights} nights = 
                                                            {formatCurrency(selectedRequest.tripOption.hotel.totalPrice)}
                                                        </div>
                                                    </div>
                                                </>
                                            )}
                                            
                                            {selectedRequest.tripOption.car && (
                                                <>
                                                    <h5>Car Rental</h5>
                                                    <div className="car-details">
                                                        <div className="car-company">{selectedRequest.tripOption.car.company}</div>
                                                        <div className="car-type">{selectedRequest.tripOption.car.carType}</div>
                                                        <div className="car-dates">
                                                            Pickup: {formatDate(selectedRequest.tripOption.car.pickupDateTime)} at {selectedRequest.tripOption.car.pickupLocation} | 
                                                            Return: {formatDate(selectedRequest.tripOption.car.dropoffDateTime)} at {selectedRequest.tripOption.car.dropoffLocation}
                                                        </div>
                                                        <div className="car-price">
                                                            {formatCurrency(selectedRequest.tripOption.car.dailyRate)} per day = 
                                                            {formatCurrency(selectedRequest.tripOption.car.totalPrice)}
                                                        </div>
                                                    </div>
                                                </>
                                            )}
                                            
                                            <div className="request-total-cost">
                                                <strong>Total Trip Cost: {formatCurrency(selectedRequest.tripOption.totalCost)}</strong>
                                            </div>
                                        </div>
                                        
                                        {selectedRequest.additionalNotes && (
                                            <div className="detail-section">
                                                <h4>Additional Notes</h4>
                                                <p>{selectedRequest.additionalNotes}</p>
                                            </div>
                                        )}
                                        
                                        <div className="detail-section approval-section">
                                            <h4>Request Approval</h4>
                                            
                                            <div className="notes-input">
                                                <label htmlFor="approval-notes">Notes:</label>
                                                <textarea 
                                                    id="approval-notes"
                                                    value={notes}
                                                    onChange={(e) => setNotes(e.target.value)}
                                                    placeholder="Add any notes about this approval/rejection..."
                                                    disabled={submitting}
                                                />
                                            </div>
                                            
                                            <div className="approval-buttons">
                                                <button 
                                                    className="approve-button"
                                                    onClick={() => submitResult(selectedRequest.requestId, TripRequestStatus.Approved)}
                                                    disabled={submitting}
                                                >
                                                    Approve
                                                </button>
                                                <button 
                                                    className="reject-button"
                                                    onClick={() => submitResult(selectedRequest.requestId, TripRequestStatus.Rejected)}
                                                    disabled={submitting}
                                                >
                                                    Reject
                                                </button>
                                            </div>
                                        </div>
                                    </div>
                                )}
                            </div>
                        )}
                    </>
                )}
            </div>
        </div>
    );
};

export default AdminPage;