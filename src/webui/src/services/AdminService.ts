import { TripRequest, TripRequestResult, TripRequestStatus } from '../types/AdminTypes';

class AdminService {
    private static instance: AdminService;
    private backendUrl: string;

    private constructor(backendUrl: string) {
        this.backendUrl = backendUrl;
    }

    static getInstance(backendUrl: string): AdminService {
        if (!AdminService.instance) {
            AdminService.instance = new AdminService(backendUrl);
        }
        return AdminService.instance;
    }

    async getRequests(): Promise<TripRequest[]> {
        try {
            const response = await fetch(`${this.backendUrl}/admin/requests`);
            if (!response.ok) {
                throw new Error(`Failed to fetch requests: ${response.statusText}`);
            }
            return await response.json();
        } catch (error) {
            console.error('Error fetching requests:', error);
            throw error;
        }
    }

    async submitResult(requestId: string, status: TripRequestStatus, notes?: string): Promise<void> {
        try {
            const result: TripRequestResult = {
                requestId,
                status,
                approvalNotes: notes || null,
                processedTime: new Date().toISOString()
            };

            const response = await fetch(`${this.backendUrl}/admin/requests/approval`, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                },
                body: JSON.stringify(result)
            });

            if (!response.ok) {
                throw new Error(`Failed to submit result: ${response.statusText}`);
            }
        } catch (error) {
            console.error('Error submitting result:', error);
            throw error;
        }
    }
}

export default AdminService;